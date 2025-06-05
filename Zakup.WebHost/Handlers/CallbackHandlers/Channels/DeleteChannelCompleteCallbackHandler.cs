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

[CallbackType(CallbackType.DeleteChannelComplete)]
public class DeleteChannelCompleteCallbackHandler : ICallbackHandler<DeleteChannelCompleteCallbackData>
{
    private readonly ChannelService _channelService;

    public DeleteChannelCompleteCallbackHandler(ChannelService channelService)
    {
        _channelService = channelService;
    }

    public async Task Handle(ITelegramBotClient botClient, DeleteChannelCompleteCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await _channelService.Delete(data.ChannelId, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.ChannelDeleted, cancellationToken: cancellationToken);
    }
}