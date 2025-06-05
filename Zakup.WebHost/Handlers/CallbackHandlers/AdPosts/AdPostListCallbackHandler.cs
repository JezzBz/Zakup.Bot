using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Posts;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.AdPosts;

[CallbackType(CallbackType.AdPostsList)]
public class AdPostListCallbackHandler : ICallbackHandler<AdPostListCallbackData>
{
    private readonly AdPostsService _adPostsService;
    private readonly HandlersManager _handlersManager;
    private readonly ChannelService _channelService;

    public AdPostListCallbackHandler(AdPostsService adPostsService, HandlersManager handlersManager, ChannelService channelService)
    {
        _adPostsService = adPostsService;
        _handlersManager = handlersManager;
        _channelService = channelService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdPostListCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        var posts = (await _adPostsService.GetPosts(data.ChannelId, callbackQuery.From.Id)).ToList();

        var buttons = new List<List<InlineKeyboardButton>>();
        
        //кнопки на посты
        if (posts.Any())
        {
            for (int i = 0; i < posts.Count(); i += 2)
            {
                var row = new List<InlineKeyboardButton>();
                for (int j = i; j < Math.Min(i + 2, posts.Count); j++)
                {
                    var callback = await _handlersManager.ToCallback(new AdPostMenuCallbackData()
                    {
                        PostId = posts[j].Id,
                    });
                    var title = string.IsNullOrEmpty(posts[j].Title) ? posts[j].Text[..4]  + "..." : posts[j].Title;
                    row.Add(InlineKeyboardButton.WithCallbackData(title, callback));
                }

                buttons.Add(row);
            }
        }

        var createPostButton = await _handlersManager.ToCallback(new CreatePostCallbackData()
        {
            ChannelId = channel!.Id
        });
        
        var backButton = await _handlersManager.ToCallback(new ShowChannelMenuCallbackData
        {
            ChannelId = channel!.Id
        });
        
        buttons.Add(new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.CreatePost, createPostButton),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backButton)
        });
        
        await botClient.SafeEdit(
            callbackQuery.From.Id,
              callbackQuery.Message!.MessageId,
            $"Канал {CommandsHelper.EscapeMarkdownV2(data.ChannelId.ToString())} {CommandsHelper.EscapeMarkdownV2(channel.Title ?? "")} [{CommandsHelper.EscapeMarkdownV2(channel.Alias ?? "")}]\nСписок креативов:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            parseMode: ParseMode.MarkdownV2, 
            cancellationToken: cancellationToken);
    }

    
}