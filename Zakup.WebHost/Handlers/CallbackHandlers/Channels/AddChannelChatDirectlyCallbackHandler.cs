using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.AddChannelChatDirectly)]
public class AddChannelChatDirectlyCallbackHandler : ICallbackHandler<AddChannelChatDirectlyCallbackData>
{
    private readonly HandlersManager _handlersManager;
    private readonly UserService _userService;
    private readonly MessagesService _messagesService;

    public AddChannelChatDirectlyCallbackHandler(HandlersManager handlersManager, UserService userService, MessagesService messagesService)
    {
        _handlersManager = handlersManager;
        _userService = userService;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, AddChannelChatDirectlyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        
        var user = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);

        if (data.RequestFirstPost && !data.Add)
        {
            await SendFirstPostMessage(botClient, callbackQuery.From.Id, data.ChannelId);
            return;
        }
            
        if(!data.RequestFirstPost && !data.Add)
        {
            await _messagesService.SendMenu(botClient, user, cancellationToken);
            return;
        }
        var skipData = await _handlersManager.ToCallback(new AddChannelChatDirectlyCallbackData
        {
            ChannelId = data.ChannelId,
            Add = false,
            RequestFirstPost = true
        });
        if(data.Add)
        {   
            var keyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithUrl(ButtonsTextTemplate.AddBot,"https://t.me/zakup_robot?startgroup&admin=invite_users"),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Skip, skipData)
            });
            
            await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.AddChannelChatText, replyMarkup:keyboard, cancellationToken: cancellationToken);
        }

        if (data.RequestFirstPost)
        {
            await SendFirstPostMessage(botClient, callbackQuery.From.Id, data.ChannelId);
        }

    }
    private async Task SendFirstPostMessage(ITelegramBotClient botClient, long userId, long channelId)
    {
        var yesData = await _handlersManager.ToCallback(new FirstAdPostCallbackData
        {
            ChannelId = channelId,
            Create = true
        });
        
        var noData = await _handlersManager.ToCallback(new FirstAdPostCallbackData
        {
            ChannelId = channelId,
            Create = false
        });
        
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Yes, yesData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Later, noData)
            }
        });

        await botClient.SendTextMessageAsync(
            userId,
            MessageTemplate.CreateFirstPostRequest,
            replyMarkup: keyboard
        );
    }
}