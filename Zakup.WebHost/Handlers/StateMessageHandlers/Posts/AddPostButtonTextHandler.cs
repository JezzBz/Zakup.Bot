using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Posts;

[StateType(UserStateType.WritePostButtonText)]
public class AddPostButtonTextHandler : IStateHandler
{
    private readonly AdPostsService _adPostService;
    private readonly UserService _userService;

    public AddPostButtonTextHandler(AdPostsService adPostService, UserService userService)
    {
        _adPostService = adPostService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        var state = await _userService.GetUserState(update.Message!.From!.Id, cancellationToken);
        var button = new TelegramPostButton
        {
            Text = message!.Text!
        };
        var data = CacheHelper.ToData<PostButtonTextCache>(state.CachedValue);
        
        await _adPostService.AddButton(data!.PostId, button);
        
        var msg = await botClient.SendTextMessageAsync(state.UserId, MessageTemplate.WriteAliasForPost, cancellationToken: cancellationToken);
        state.Clear();
        state.CachedValue = CacheHelper.ToCache(new AddPostAliasCache
        {
            PostId = data.PostId
        });
        state.State = UserStateType.AddPostTitle;
        
        state.PreviousMessageId = msg.MessageId;
        await _userService.SetUserState(update.Message!.From!.Id, state, cancellationToken);
    }
}