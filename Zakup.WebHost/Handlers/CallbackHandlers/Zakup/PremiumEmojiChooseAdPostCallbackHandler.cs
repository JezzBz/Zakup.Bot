using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.PremiumEmojiChooseAdPost)]
public class PremiumEmojiChooseAdPostCallbackHandler : ICallbackHandler<PremiumEmojiChooseAdPostCallbackData>
{
    private readonly AdPostsService _adPostsService;
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public PremiumEmojiChooseAdPostCallbackHandler(AdPostsService adPostsService, ZakupService zakupService, HandlersManager handlersManager)
    {
        _adPostsService = adPostsService;
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, PremiumEmojiChooseAdPostCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(data.ZakupId,cancellationToken:cancellationToken);
        var adPosts = await _adPostsService.GetPosts(zakup.ChannelId, cancellationToken);
        var buttons = new List<List<InlineKeyboardButton>>();
        
        if (adPosts.Any())
        {
            for (int i = 0; i < adPosts.Count(); i += 2)
            {
                var row = new List<InlineKeyboardButton>();
                for (int j = i; j < Math.Min(i + 2, adPosts.Count); j++)
                {
                    var callback = await _handlersManager.ToCallback(new PremiumEmojiCallbackData()
                    {
                        AdPostId = adPosts[j].Id,
                        ZakupId = data.ZakupId
                    });
                    
                    var title = string.IsNullOrEmpty(adPosts[j].Title) ? adPosts[j].Text[..4]  + "..." : adPosts[j].Title;
                    
                    row.Add(InlineKeyboardButton.WithCallbackData(title, callback));
                }

                buttons.Add(row);
            }
        }

        var createPostButton = await _handlersManager.ToCallback(new CreatePostCallbackData()
        {
            ChannelId = zakup.ChannelId
        });
        buttons.Add([
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.CreatePost, createPostButton)
        ]);

        await botClient.SafeEdit(callbackQuery.From.Id, 
            callbackQuery.Message.MessageId,
            MessageTemplate.ChooseZakupCreative, 
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: cancellationToken);
    }

    public PremiumEmojiChooseAdPostCallbackData Parse(List<string> parameters)
    {
        return new PremiumEmojiChooseAdPostCallbackData
        {
            ZakupId = Guid.Parse(parameters[0]),
        };
    }
}