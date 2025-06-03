using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Zakup;

[StateType(UserStateType.PdpCheck)]
public class PdpCheckHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;

    public PdpCheckHandler(UserService userService, HandlersManager handlersManager)
    {
        _userService = userService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        
        if (message!.ForwardFromChat is null || message.ForwardFromChat.Type != ChatType.Channel)
        {
            Console.WriteLine("Message is not a forwarded message from a channel.");
            await botClient.SendTextMessageAsync(
                message!.From!.Id,
                MessageTemplate.PDPBadMessageError, cancellationToken: cancellationToken);
            return;
        }

        // Если переслал бот, просим именно от канала
        if (message.ForwardFrom is not null && message.ForwardFrom.IsBot)
        {
            Console.WriteLine("Message was forwarded from a bot.");
            await botClient.SendTextMessageAsync(
                message!.From!.Id,
                MessageTemplate.PDPBotMessageError, cancellationToken: cancellationToken);
            return;
        }

        var state = await _userService.GetUserState(message.From!.Id, cancellationToken);

        var zakupId = CacheHelper.ToData<ZakupIdCache>(state!.CachedValue!)!.ZakupId;
        long verifiedChannelId = message.ForwardFromChat.Id;
        
        IEnumerable<ChatMember> admins;
        try
        {
            admins = await botClient.GetChatAdministratorsAsync(verifiedChannelId, cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            await botClient.SendTextMessageAsync(message.From.Id, MessageTemplate.PDPNoBotInChannels, cancellationToken: cancellationToken);
            Console.WriteLine($"Ошибка получения администраторов: {ex.Message}");
            return;
        }
        bool hasChat = false;
        foreach (var admin in admins)
        {
            try
            {
                // Пробуем отправить короткое уведомление (можно использовать SendChatAction)
                await botClient.SendChatActionAsync(admin.User.Id, ChatAction.Typing, cancellationToken: cancellationToken);
                hasChat = true;
                break;
            }
            catch (ApiRequestException ex)
            {
                Console.WriteLine($"Не удалось отправить действие в чат админу {admin.User.Id}: {ex.Message}");
            }
        }
        
        if (!hasChat)
        {
            await botClient.SendTextMessageAsync(message.From.Id, MessageTemplate.PDPNoChatWithAdmins, cancellationToken: cancellationToken);
            return;
        }
        int sentCount = 0;
        var pdpAcceptCallbackData = _handlersManager.ToCallback(new PDPVerificationCallbackData
        {
            RequestUserId = message.From.Id,
            ChannelId = verifiedChannelId,
            PlacementId = zakupId
        });
        
        foreach (var admin in admins)
        {
            
            try
            {
                await botClient.SendTextMessageAsync(
                    admin.User.Id,
                    MessageTemplate.PDPRequestNotificationMessage(zakupId),
                    replyMarkup: new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                    {
                        new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.AcceptPDP, pdpAcceptCallbackData)
                        }
                    }), cancellationToken: cancellationToken);
                sentCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки запроса админу {admin.User.Id}: {ex.Message}");
            }
        }
        
        if (sentCount == 0)
        {
            // Ни одному администратору не удалось отправить сообщение
            await botClient.SendTextMessageAsync(
                message.From.Id,
                MessageTemplate.PDPError, cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(message.From.Id, MessageTemplate.PDPRequestSent, cancellationToken: cancellationToken);
        }
    }
}