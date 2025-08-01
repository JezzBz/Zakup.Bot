using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;
using Zakup.WebHost.Helpers.Callback;

namespace Zakup.WebHost.Handlers.MessageHandlers;

public class StartMessageHandler : IUpdatesHandler
{
    private readonly UserService _userService;
    private readonly MessagesService _messagesService;
    
    public StartMessageHandler(UserService userService, MessagesService messagesService)
    {
        _userService = userService;
        _messagesService = messagesService;
    }

    public static bool ShouldHandle(Update update)
    {
        var baseMessageCondition = update.IsNotEmptyMessage() &&
                            (update.Message?.Text?.StartsWith("/start") ?? false);
        
        var baseCallbackCondition = update.IsCallback() && (update.CallbackQuery?.Data?.StartsWith("menu") ?? false);
        
        return (baseMessageCondition  &&
               !update.Message!.From!.IsBot &&
               update.Message.Chat.Type == ChatType.Private)  || baseCallbackCondition;
    }

    public  async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(update.Message?.From?.Id ?? update.CallbackQuery!.From.Id, cancellationToken);
        
        if (user == null)
        { 
            await HandleStart(botClient, update, cancellationToken);
            return;
        }
        
        if (user?.UserState == null)
        {
            await _userService.SetUserState(user!.Id, new TelegramUserState(), cancellationToken);
            user = await _userService.GetUser(update.Message?.From?.Id ?? update.CallbackQuery!.From.Id, cancellationToken); //FIx for merge
        }
        
        if (user?.UserState?.MenuMessageId != null)
        { 
            await botClient.SafeDelete(update.Message?.Chat.Id ?? update.CallbackQuery!.From.Id, user.UserState.MenuMessageId.Value, cancellationToken);   
        }
        
        if (update.CallbackQuery?.Data?.StartsWith("menu") ?? false)
        {
            var msgId = int.Parse(update.CallbackQuery?.Data.Split("|")[1]!);
            await _messagesService.SendMenu(botClient, user!, cancellationToken, msgId);
            return;
        }
      
        
        await _messagesService.SendMenu(botClient, user!, cancellationToken);
        
        await botClient.SafeDelete(update.Message!.Chat.Id, update.Message.MessageId, cancellationToken);
    }

    private async Task HandleStart(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var commandText = update.Message!.Text!;
        var refer = CommandsHelper.ParseCommandValue(commandText, "/start");
        var user = await _userService.CreateUser(update.Message!.From!.Id, update.Message!.From!.Username!, refer, cancellationToken);
            
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
            
            user.UserState.MenuMessageId = msg.MessageId;
            user.UserState.PreviousMessageId = msg.MessageId;
            user.UserState.State = UserStateType.ConfirmAddChannel;
            await _userService.SetUserState(user.Id, user.UserState, cancellationToken);
            
            
            
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            Console.WriteLine($"Ошибка Содержимое сообщения: ID {update.Message.MessageId}, текст '{update.Message.Text}', chat ID {update.Message.Chat.Id}, user ID {update.Message.From?.Id}, username {update.Message.From?.Username}");
            throw; 
        } 
    }

   
    
}