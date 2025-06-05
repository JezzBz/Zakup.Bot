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

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Posts;

[CallbackType(CallbackType.AdPostMenu)]
public class AdPostMenuCallbackHandler : ICallbackHandler<AdPostMenuCallbackData>
{
    private readonly AdPostsService _adPostsService;
    private readonly HandlersManager _handlersManager;

    public AdPostMenuCallbackHandler(AdPostsService adPostsService, HandlersManager handlersManager)
    {
        _adPostsService = adPostsService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, AdPostMenuCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var post = await _adPostsService.Get(data.PostId, cancellationToken);

        var deletePostData = await _handlersManager.ToCallback(new DeleteAdPostCallbackData
        {
            PostId = data.PostId
        });
        
        var generatePostData = await _handlersManager.ToCallback(new GenerateAdPostCallbackData()
        {
            PostId = data.PostId
        });
        
        var backPostData = await _handlersManager.ToCallback(new AdPostListCallbackData()
        {
            ChannelId = post!.ChannelId
        });
        
        var buttons = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Delete, deletePostData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Watch, generatePostData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backPostData)
        };
        
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, $"Креатив {post.Title} [{post.Id}]", replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
    }
}