using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ZakupCreate)]
public class ZakupCreateCallbackHandler : ICallbackHandler<EmptyCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;
    private readonly UserService _userService;

    public ZakupCreateCallbackHandler(ChannelService channelService, HandlersManager handlersManager, UserService userService)
    {
        _channelService = channelService;
        _handlersManager = handlersManager;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
       
        var channels = await _channelService.GetChannels(callbackQuery.From.Id, cancellationToken);
        var buttons = new List<List<InlineKeyboardButton>>();
        for (int i = 0; i < channels.Count; i += 2)
        {
            var buttonData = _handlersManager.ToCallback(new AddZakupDateCallbackData()
            {
                ChannelId = channels[i].Id
            });
            
            var row = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"[{channels[i].Alias}] {channels[i].Title}", buttonData)
            };
            if (i + 1 < channels.Count)
            {
                row.Add(InlineKeyboardButton.WithCallbackData($"[{channels[i + 1].Alias}] {channels[i + 1].Title}", buttonData));
            }
            buttons.Add(row);
        }
        buttons.Add(new List<InlineKeyboardButton>(
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, $"menu|{callbackQuery.Message.MessageId}"),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.NewChannel, nameof(CallbackType.NewChannel))
            }
        ));
            
        await botClient.SafeEdit(callbackQuery.From.Id,
            callbackQuery.Message!.MessageId, 
            MessageTemplate.ChooseZakupChannel, 
            replyMarkup: new InlineKeyboardMarkup(buttons), 
            cancellationToken: cancellationToken);
    }

    public EmptyCallbackData Parse(List<string> parameters)
    {
        return new EmptyCallbackData();
    }
}