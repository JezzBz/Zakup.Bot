using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.MarkAsLead)]
public class MarkAsLeadCallbackHandler : ICallbackHandler<MarkAsLeadCallbackData>
{
    private readonly UserService _userService;

    public MarkAsLeadCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, MarkAsLeadCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await _userService.MarkAsLead(callbackQuery.From.Id, data.LeadUserId, cancellationToken);
        
        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id, 
            callbackQuery.Message!.MessageId, 
            callbackQuery.Message!.Text!, 
            cancellationToken: cancellationToken);
    }

    public MarkAsLeadCallbackData Parse(List<string> parameters)
    {
        return new MarkAsLeadCallbackData()
        {
            LeadUserId = long.Parse(parameters[0]),
        };
    }
}