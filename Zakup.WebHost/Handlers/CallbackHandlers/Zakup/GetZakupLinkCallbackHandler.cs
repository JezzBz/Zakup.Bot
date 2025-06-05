using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.GetZakupLink)]
public class GetZakupLinkCallbackHandler : ICallbackHandler<GetZakupLinkCallbackData>
{
    private readonly ZakupService _zakupService;

    public GetZakupLinkCallbackHandler(ZakupService zakupService)
    {
        _zakupService = zakupService;
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
        
        //TODO: добавить таблицы
        
        var zakupMessage = MessageTemplate.ZakupSummaryMessage(zakup.Channel.Title, zakup.Price, zakup.PostTime, zakup.AdPost?.Title, zakup.IsPad);
        await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message!.MessageId,cancellationToken);
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, zakupMessage, cancellationToken: cancellationToken); //TODO: Добавить markup 
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.YouLinkMessage(zakup.InviteLink), cancellationToken: cancellationToken);
    }

    public GetZakupLinkCallbackData Parse(List<string> parameters)
    {
        return new GetZakupLinkCallbackData
        {
            ZakupId = Guid.Parse(parameters[0]),
        };
    }
}