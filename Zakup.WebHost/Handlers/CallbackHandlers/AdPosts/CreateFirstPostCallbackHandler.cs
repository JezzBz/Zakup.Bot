using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.AdPosts;

[CallbackType(CallbackType.FirstAdPost)]
public class CreateFirstPostCallbackHandler : ICallbackHandler<FirstAdPostCallbackData>
{
    private readonly UserService _userService;
    private readonly MessagesService _messagesService;
    public CreateFirstPostCallbackHandler(UserService userService, MessagesService messagesService)
    {
        _userService = userService;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, FirstAdPostCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);
        if (!data.Create)
        {
            await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message.MessageId, cancellationToken);
            await botClient.SendTextMessageAsync(
                callbackQuery.From.Id,
                MessageTemplate.FirstPostDecline,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
            await _messagesService.SendMenu(botClient, user, cancellationToken);
            return;
        }
        
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        var msg = await botClient.SafeEdit(state!.UserId, callbackQuery.Message!.MessageId,
            MessageTemplate.CreativeInstructionText, cancellationToken: cancellationToken);
        state.State = UserStateType.CreateNewPost;
        state.CachedValue = CacheHelper.ToCache(new NewPostCache
        {
            ChannelId = data.ChannelId
        });
        state.PreviousMessageId = msg.MessageId;
        await _userService.SetUserState(state, cancellationToken);
    }
}