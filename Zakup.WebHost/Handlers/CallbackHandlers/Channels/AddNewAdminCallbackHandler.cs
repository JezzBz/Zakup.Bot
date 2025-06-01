using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.AddNewChannelAdmin)]
public class AddNewAdminCallbackHandler : ICallbackHandler<AddNewAdminCallbackData>
{
    private readonly UserService _userService;

    public AddNewAdminCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AddNewAdminCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userState = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.AddAdminRequest, cancellationToken: cancellationToken);
        userState.State = UserStateType.AddAdmin;
        userState.CachedValue = CacheHelper.ToCache(new ChannelIdCache
        {
            ChannelId = data.ChannelId
        });
        await _userService.SetUserState(userState, cancellationToken);
    }

    public AddNewAdminCallbackData Parse(List<string> parameters)
    {
        return new AddNewAdminCallbackData
        {
            ChannelId = long.Parse(parameters[0]),
        };
    }
}