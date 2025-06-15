using Bot.Core;
using Mono.TextTemplating;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.DTO.Channel;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers;

public class BotChatMemberHandler : IUpdatesHandler
{
    private readonly HandlersManager _handlersManager;
    private readonly ChannelService _channelService;
    public BotChatMemberHandler(HandlersManager handlersManager, ChannelService channelService)
    {
        _handlersManager = handlersManager;
        _channelService = channelService;
    }

    public static bool ShouldHandle(Update update)
    {
        return update.MyChatMember is not null;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.MyChatMember.Chat.Type == ChatType.Channel)
        {
            await ProcessChannelUpdate(botClient, update, cancellationToken);
            return;
        }

        if (update.MyChatMember.Chat.Type == ChatType.Group || update.MyChatMember.Chat.Type == ChatType.Supergroup)
        {
            await ProcessGroupUpdate(botClient, update, cancellationToken);
            return;
        }
    }

    private async Task ProcessChannelUpdate(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var me = await botClient.GetMeAsync();
        var cmu = update.MyChatMember;
        var botId = me.Id;

        Console.WriteLine($"Bot ID: {botId}");
        Console.WriteLine($"NewChatMember Status: {cmu.NewChatMember.Status}");
        Console.WriteLine($"OldChatMember Status: {cmu.OldChatMember?.Status}");
        
        // Проверяем, что бот стал администратором
        if (cmu.NewChatMember.User.Id == botId
            && cmu.NewChatMember.Status == ChatMemberStatus.Administrator
            && cmu.OldChatMember?.Status != ChatMemberStatus.Administrator)
        {
            Console.WriteLine("Bot has been promoted to Administrator.");

            var callbackData = await _handlersManager.ToCallback(new AddChannelDirectlyCallbackData
            {
                ChannelId = cmu.Chat.Id,
                ChannelTitle = cmu.Chat.Title
            });
            
            
            var confirmButton = InlineKeyboardButton.WithCallbackData(
                text: ButtonsTextTemplate.Approve,
                callbackData: callbackData
            );

            var channelName = string.IsNullOrEmpty(cmu.Chat.Title) ? "неизвестный канал" : cmu.Chat.Title;

            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: cmu.From.Id,
                    text: MessageTemplate.BotChatMemberText(CommandsHelper.EscapeMarkdownV2(channelName)),
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: new InlineKeyboardMarkup(confirmButton), cancellationToken: cancellationToken);
                Console.WriteLine("Confirm button sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке кнопки подтверждения: {ex.Message}");
                // Можно добавить дополнительную обработку ошибки, например, логирование
            }
        }
        else
        {
            Console.WriteLine("Bot status did not change to Administrator.");
        }
    }
 
    private async Task ProcessGroupUpdate(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var me = await botClient.GetMeAsync();
        var cmu = update.MyChatMember;
        var botId = me.Id;

        Console.WriteLine($"Bot ID: {botId}");
        Console.WriteLine($"NewChatMember Status: {cmu.NewChatMember.Status}");
        Console.WriteLine($"OldChatMember Status: {cmu.OldChatMember?.Status}");

        var adminCondition = cmu.NewChatMember.User.Id == botId
                             && cmu.NewChatMember.Status == ChatMemberStatus.Administrator
                             && cmu.OldChatMember?.Status != ChatMemberStatus.Administrator;
        // Проверяем, что бот стал администратором
        if (!adminCondition)
        {
            Console.WriteLine("Bot status did not change to Administrator.");
            return;
        }

        var chat = await botClient.GetChatAsync(cmu.Chat.Id, cancellationToken: cancellationToken);
        if (chat.LinkedChatId != null)
        {
            await _channelService.AddChannelChat(chat.LinkedChatId.Value, cmu.Chat.Id, cancellationToken);
            
            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: cmu.From.Id,
                    text: MessageTemplate.AddedChannelChat,
                    parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);
                    
                Console.WriteLine("Confirm button sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке уведомления о подключенни чата: {ex.Message}");
                // Можно добавить дополнительную обработку ошибки, например, логирование
            }
            
        }
        
    }
}