using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;
using Zakup.WebHost.Services;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.GetZakupLink)]
public class GetZakupLinkCallbackHandler : ICallbackHandler<GetZakupLinkCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly InternalSheetsService _sheetsService;
    private readonly ZakupMessageService _zakupMessageService;
    public GetZakupLinkCallbackHandler(ZakupService zakupService, InternalSheetsService sheetsService, ZakupMessageService zakupMessageService)
    {
        _zakupService = zakupService;
        _sheetsService = sheetsService;
        _zakupMessageService = zakupMessageService;
    }

    public async Task Handle(ITelegramBotClient botClient, GetZakupLinkCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(data.ZakupId, true,cancellationToken: cancellationToken);
        var link = await botClient.CreateChatInviteLink(
            zakup.ChannelId,
            name: $"Без крео {zakup.Price}",
            createsJoinRequest: zakup.NeedApprove);
        zakup.Accepted = true;
        zakup.InviteLink = link.InviteLink;
        await _zakupService.Update(zakup, cancellationToken);
        
        await _sheetsService.AppendRowByHeaders(callbackQuery.From.Id, zakup.ChannelId, new Dictionary<string, object>()
        {
            ["Дата создания закупа"] = zakup.PostTime?.AddHours(3).ToString("dd.MM.yyyy HH:mm") ?? DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm"),
            ["Платформа"] = zakup.Platform ?? "",
            ["Цена"] = zakup.Price,
            ["Креатив"] = "Без крео",
            ["Оплачено"] = zakup.IsPad ? "Да" : "Нет",
            ["Пригласительная ссылка (не удалять)"] = zakup.InviteLink ?? "",
            ["Сейчас в канале"] = 0,
            ["Покинуло канал"] = 0,
            ["Цена за подписчика(оставшегося)"] = 0,
            ["Отписываемость первые 48ч(% от отписавшихся)"] = 0,
            ["Премиум пользователей"] = 0,
            ["Подписчиков 7+ дней(% от всего вступивших)"] = 0,
            ["Клиентов по ссылке"] = 0,
            ["Комментирует из подписавшихся(%)"] = 0,
        });
        
        var keyboard = await _zakupMessageService.GetEditMenuKeyboard(data.ZakupId, cancellationToken);
        var zakupMessage = MessageTemplate.ZakupSummaryMessage(zakup.Channel.Title, zakup.Price, zakup.PostTime, zakup.AdPost?.Title, zakup.IsPad);
        await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message!.MessageId,cancellationToken);
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, zakupMessage, replyMarkup:keyboard, cancellationToken: cancellationToken); 
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.YouLinkMessage(zakup.InviteLink), cancellationToken: cancellationToken);
    }
}