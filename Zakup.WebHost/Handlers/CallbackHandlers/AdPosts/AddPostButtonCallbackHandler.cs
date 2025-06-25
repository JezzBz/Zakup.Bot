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

[CallbackType(CallbackType.AddPostButton)]
public class AddPostButtonCallbackHandler : ICallbackHandler<AddPostButtonCallbackData>
{
    private readonly UserService _userService;

    public AddPostButtonCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AddPostButtonCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        if (data.Add)
        {
           
            await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,
                MessageTemplate.WriteButtonText, cancellationToken: cancellationToken);
            state.State = UserStateType.WritePostButtonText;
            state.CachedValue = CacheHelper.ToCache(new PostButtonTextCache
            {
                PostId = data.AdPostId
            });
            await _userService.SetUserState(state, cancellationToken);
            return;
        }
        
        var msg = await botClient.SendTextMessageAsync(state.UserId, MessageTemplate.WriteAliasForPost, cancellationToken: cancellationToken);
        state.Clear();
        state.CachedValue = CacheHelper.ToCache(new AddPostAliasCache
        {
            PostId = data.AdPostId
        });
        
        state.State = UserStateType.AddPostTitle;
        state.PreviousMessageId = msg.MessageId;
        await _userService.SetUserState(callbackQuery!.From!.Id, state, cancellationToken);
        await botClient.SafeDelete(callbackQuery.From.Id, callbackQuery.Message!.MessageId, cancellationToken);
       
    }
}