using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.AdPosts;

[CallbackType(CallbackType.DeleteAdPost)]
public class DeleteAdPostCallbackHandler : ICallbackHandler<DeleteAdPostCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, DeleteAdPostCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public DeleteAdPostCallbackData Parse(List<string> parameters)
    {
        return new DeleteAdPostCallbackData
        {
            PostId = Guid.Parse(parameters[0])
        };
    }
}