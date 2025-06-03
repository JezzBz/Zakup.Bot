using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.PDPApprove)]
public class PDPVerificationCallbackHandler : ICallbackHandler<PDPVerificationCallbackData>
{
    private readonly ZakupService _zakupService;

    public PDPVerificationCallbackHandler(ZakupService zakupService)
    {
        _zakupService = zakupService;
    }

    public async Task Handle(ITelegramBotClient botClient, PDPVerificationCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var members = await _zakupService.GetMembers(data.PlacementId, cancellationToken);
        
        int total = members.Count;
        int verifiedCount = 0;
        foreach (var member in members)
        {
            try
            {
                // Если пользователь найден (даже если статус — left), считаем его подтверждённым
                var chatMember = await botClient.GetChatMemberAsync(data.ChannelId, member.UserId);
                verifiedCount++;
            }
            catch (ApiRequestException)
            {
                // Если пользователь не найден в канале, пропускаем
            }
        }
        var percentage = total > 0 ? (verifiedCount / (double)total) * 100 : 0;
        var resultText = MessageTemplate.PdpVerificationResultMessage(total, verifiedCount, percentage);
        // Отправляем результат как администратору, подтвердившему запрос, так и заказчику сверки
        await botClient.SendTextMessageAsync(data.RequestUserId, resultText, cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, resultText, cancellationToken: cancellationToken);
        await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message!.MessageId, cancellationToken);
    }

    public PDPVerificationCallbackData Parse(List<string> parameters)
    {
        return new PDPVerificationCallbackData()
        {
            RequestUserId = long.Parse(parameters[0]),
            ChannelId = long.Parse(parameters[1]),
            PlacementId = Guid.Parse(parameters[2]),
        };
    }
}