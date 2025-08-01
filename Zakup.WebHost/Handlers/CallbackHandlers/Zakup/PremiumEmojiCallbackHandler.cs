using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.PremiumEmoji)]
public class PremiumEmojiCallbackHandler : ICallbackHandler<PremiumEmojiCallbackData>
{
    private readonly ZakupService _zakupService;

    public PremiumEmojiCallbackHandler(ZakupService zakupService)
    {
        _zakupService = zakupService;
    }

    public async Task Handle(ITelegramBotClient botClient, PremiumEmojiCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup =  await _zakupService.Get(data.ZakupId,true,cancellationToken);
        zakup.AdPostId = data.AdPostId;
        zakup = await _zakupService.Update(zakup, cancellationToken);
        await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message!.MessageId, cancellationToken);
        var imageUrl =
            new Uri(
                "https://i.imgur.com/Q81nxWe.png");

        var buttons = new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(ButtonsTextTemplate.CreateFromInline, $"{zakup.Platform} {zakup.Price} {zakup.Channel!.Alias!} {zakup.AdPost.Title}"),
        };
        await botClient.SendPhotoAsync(
            
            callbackQuery.From.Id,
            new InputFileUrl(imageUrl),
            caption: MessageTemplate.PremiumEmogiText,
            replyMarkup:new InlineKeyboardMarkup(buttons),
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}