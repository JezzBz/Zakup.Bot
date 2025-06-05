using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.AdPosts;

[CallbackType(CallbackType.DeleteAdPost)]
public class DeleteAdPostCallbackHandler : ICallbackHandler<DeleteAdPostCallbackData>
{
    private readonly AdPostsService _adPostsService;
    private readonly MessagesService _messagesService;
    private readonly UserService _userService;

    public DeleteAdPostCallbackHandler(AdPostsService adPostsService, MessagesService messagesService, UserService userService)
    {
        _adPostsService = adPostsService;
        _messagesService = messagesService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, DeleteAdPostCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var adPost = await _adPostsService.Get(data.PostId, cancellationToken);
        if (adPost == null)
        {
            await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message!.MessageId, cancellationToken);
            return;
        }
        await _adPostsService.Delete(adPost, cancellationToken);
        var user = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);
        await _messagesService.SendMenu(botClient, user! ,cancellationToken, callbackQuery.Message!.MessageId);
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.AdPostDeleted, cancellationToken: cancellationToken);
    }
}