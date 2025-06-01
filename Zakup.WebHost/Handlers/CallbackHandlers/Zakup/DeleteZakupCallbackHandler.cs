using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.DeleteZakup)]
public class DeleteZakupCallbackHandler  : ICallbackHandler<DeleteZakupCallbackData>
{
    private readonly ZakupService _service;
    private readonly MessagesService _messagesService;
    private readonly UserService _userService;

    public DeleteZakupCallbackHandler(ZakupService service, MessagesService messagesService, UserService userService)
    {
        _service = service;
        _messagesService = messagesService;
        _userService = userService;
    }

    public  async Task Handle(ITelegramBotClient botClient, DeleteZakupCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);
        await _service.Delete(data.ZakupId, cancellationToken);
        await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message!.MessageId, cancellationToken);
        await _messagesService.SendMenu(botClient, user, cancellationToken);
    }

    public DeleteZakupCallbackData Parse(List<string> parameters)
    {
        return new DeleteZakupCallbackData()
        {
            ZakupId = Guid.Parse(parameters[0])
        };
    }
}