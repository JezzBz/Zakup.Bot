using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Posts;

[StateType(UserStateType.CreateNewPost)]
public class NewPostHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly MessagesService _messagesService;
    private readonly AdPostsService _adPostsService;
    private readonly DocumentsStorageService _documentsStorageService;
    private readonly HandlersManager _handlersManager;

    public NewPostHandler(UserService userService, MessagesService messagesService, AdPostsService adPostsService, DocumentsStorageService documentsStorageService, HandlersManager handlersManager)
    {
        _userService = userService;
        _messagesService = messagesService;
        _adPostsService = adPostsService;
        _documentsStorageService = documentsStorageService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(update.Message!.From!.Id, cancellationToken);
        var message = update.Message;
        var messageText = message.Text ?? message.Caption ?? "";
        
        if (messageText!.Contains(SystemConstants.CancelCommand))
        {
            await botClient.SafeDelete(update.Message.Chat.Id, user!.UserState!.PreviousMessageId, cancellationToken);
            user.UserState.Clear();
            await _userService.SetUserState(user.UserState, cancellationToken);
            await _messagesService.SendMenu(botClient, user, cancellationToken);
            return;
        }
        if (!await ValidateMessage(botClient, update.Message, update.Message.Chat.Id))
        {
            return;
        }

        var channelId = CacheHelper.ToData<NewPostCache>(user!.UserState.CachedValue!)!.ChannelId;
        
        var adPost = new TelegramAdPost()
        {
            Text = messageText,
            ChannelId = channelId,
            Entities = (message.Entities ?? message.CaptionEntities ?? Array.Empty<MessageEntity>()).ToList(),
            Buttons = new List<TelegramPostButton>(),
            Title = "",
            Id = Guid.NewGuid(),
        };
        
        var entity = await _adPostsService.SavePost(adPost);
        await _documentsStorageService.SaveDocuments(botClient, message, message.MediaGroupId!);
        if(message.MediaGroupId != null) 
            await _documentsStorageService.AttachMediaGroupToPost(message.MediaGroupId!, entity.Id);
        
        var markup = await GetKeyboardMarkup(adPost.Id);

        await botClient.SafeDelete(user.Id, user.UserState.PreviousMessageId, cancellationToken);
        await botClient.SendTextMessageAsync(user.Id, 
            MessageTemplate.AddButtonQuestion, 
            replyMarkup: markup, 
            cancellationToken: cancellationToken);
        
        user.UserState.Clear();
        await _userService.SetUserState(user.UserState, cancellationToken);

    }

    private async Task<bool> ValidateMessage(ITelegramBotClient botClient, Message message, long userId)
    {
        var allEntities = (message.Entities ?? Array.Empty<MessageEntity>())
            .Concat(message.CaptionEntities ?? Array.Empty<MessageEntity>())
            .ToList();

		
        if (allEntities.Any(x => x.Type == 0))
        {
            // Вот тут пишем сообщение об ошибке и делаем return
            await botClient.SendTextMessageAsync(
                userId, 
                MessageTemplate.BadFormat
            );
            return false;
        }

        if (message.Caption != null && message.Caption.Length > 1023)
        {
            await botClient.SendTextMessageAsync(userId, MessageTemplate.CaptionTooLong);
            return false; // Прекращаем выполнение, если длина подписи превышает лимит
        }

        if (message.Text != null && message.Text.Length > 4095)
        {
            await botClient.SendTextMessageAsync(userId, MessageTemplate.MessageTooLong);
            return false; // Прекращаем выполнение, если длина подписи превышает лимит
        }

        if (message.Document?.FileSize > 20_000_000 || message.Video?.FileSize > 20_000_000)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, MessageTemplate.FileTooHeavy);
            return false;
        }
        
        var text = message.Text ?? message.Caption ?? "";
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            await botClient.SendTextMessageAsync(userId, MessageTemplate.EmptyTextError);
            return false;
        }

        return true;
    }

    private async Task<InlineKeyboardMarkup> GetKeyboardMarkup(Guid postId)
    {
        var trueData = await _handlersManager.ToCallback(new AddPostButtonCallbackData
        {
            AdPostId = postId,
            Add = true
        });
        
        var falseData = await _handlersManager.ToCallback(new AddPostButtonCallbackData
        {
            AdPostId = postId,
            Add = false
        });
        
        return new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Yes, trueData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.No, falseData)
        });
    } 
}