using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.DTO;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;
using Zakup.WebHost.Helpers.Callback;

namespace Zakup.WebHost.Handlers.MessageHandlers;

public class StartMessageHandler : IUpdatesHandler
{
    private readonly UserService _userService;
    private readonly ZakupService _zakupService;
    private readonly ChannelService _channelService;
    
    public StartMessageHandler(UserService userService, ZakupService zakupService, ChannelService channelService)
    {
        _userService = userService;
        _zakupService = zakupService;
        _channelService = channelService;
    }

    public static bool ShouldHandle(Update update)
    {
        var baseMessageCondition = update.IsNotEmptyMessage() &&
                            update.Message!.Text!.StartsWith("/start");
        var baseCallbackCondition = update.IsCallback() && update.CallbackQuery?.Data == "menu";
        
        return (baseMessageCondition || baseCallbackCondition) &&
               !update.Message!.From!.IsBot &&
               update.Message.Chat.Type == ChatType.Private;
    }

    public  async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(update.Message!.From!.Id, cancellationToken);
        if (user == null)
        { 
            await HandleStart(botClient, update, cancellationToken);
            return;
        }
        await HandleMenu(botClient, update, user, cancellationToken);
    }

    private async Task HandleStart(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var commandText = update.Message!.Text!;
        var refer = CommandsHelper.ParseCommandValue(commandText, "/start");
        var user = await _userService.CreateUser(update.Message!.From!.Id, update.Message!.From!.Username!, refer);
            
        try
        {
            var keyBoard =
                InternalKeyboard.FromButton(InlineKeyboardButton.WithUrl(ButtonsTextTemplate.AddBot,
                    SystemConstants.InviteUrl));
            var msg = await botClient.SendTextMessageAsync(
                chatId: user.Id,
                text: MessageTemplate.Welcome,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyBoard, cancellationToken: cancellationToken);
            //await ProceedToNextState(BotFlows.RECEVIER_CHANNEL_MESSAGE, msg.MessageId);
            await botClient.SafeDelete(update.Message.Chat.Id, update.Message.MessageId);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            Console.WriteLine($"Ошибка Содержимое сообщения: ID {update.Message.MessageId}, текст '{update.Message.Text}', chat ID {update.Message.Chat.Id}, user ID {update.Message.From?.Id}, username {update.Message.From?.Username}");
            throw; 
        } 
    }

    private async Task HandleMenu(ITelegramBotClient botClient, Update update, TelegramUser user ,CancellationToken cancellationToken)
    {
        var period = TimeHelper.GetToday();
        var zakupStatistic = await _zakupService.GetStatistics(user.Id, period.StartTime, period.EndTime, cancellationToken);
        var channelsStatistic = await _channelService.GetSubscribeStatistic(user.Id, period.StartTime, period.EndTime, cancellationToken);
        var menuMessage = MessageTemplate.GetMenu(zakupStatistic.Price, zakupStatistic.PaidPrice, channelsStatistic.SubscribeCount, channelsStatistic.SubscribeCount);
        await botClient.SendTextMessageAsync(user.Id, menuMessage, parseMode:ParseMode.Markdown, cancellationToken: cancellationToken);
        //Добавить админку и трекинг меню-сообщений
    }

    
}