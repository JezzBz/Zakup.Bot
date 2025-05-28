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

[CallbackType(CallbackType.CreatePost)]
public class CreatePostCallbackHandler : ICallbackHandler<CreatePostCallbackData>
{
    private readonly UserService _userService;

    public CreatePostCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, CreatePostCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
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

    public CreatePostCallbackData Parse(List<string> parameters)
    {
        return new CreatePostCallbackData()
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}