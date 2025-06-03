using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ChannelStatistic)]
public class ChannelStatisticCallbackHandler : ICallbackHandler<ChannelStatisticCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ChannelStatisticCallbackHandler(ZakupService zakupService, HandlersManager handlersManager)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ChannelStatisticCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var take = 5;
        var skip = (data.PageNumber - 1) * take;
        
        var zakups = await _zakupService.GetList(data.ChannelId, skip, take + 1, cancellationToken);
        var isLastPage = zakups.Count < take + 1;
        if (!isLastPage)
        {
            zakups.RemoveAt(zakups.Count - 1);
        }
        
        if (zakups.Count == 0 && data.PageNumber == 1)
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.NoZakupsForThisChannel,
                cancellationToken: cancellationToken);
             return;
        }
        var buttons = new List<List<InlineKeyboardButton>>();

        foreach (var placement in zakups)
        {
            var buttonText = $"{placement.CreatedUtc:dd.MM.yyyy} {placement.Platform} {placement.Price}";
            var callbackData = _handlersManager.ToCallback(new ZakupStatisticCallbackData
            {
                ZakupId = placement.Id
            });
            
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(buttonText, callbackData)
            });
        }
        
        var paginationButtons = new List<InlineKeyboardButton>();
        
        if (data.PageNumber > 1)
        {
            var backCallback = _handlersManager.ToCallback(new ChannelStatisticCallbackData
            {
                ChannelId = data.ChannelId,
                PageNumber = data.PageNumber - 1,
            });
            
            paginationButtons.Add(InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backCallback));
        }

        if (!isLastPage)
        {
            var backCallback = _handlersManager.ToCallback(new ChannelStatisticCallbackData
            {
                ChannelId = data.ChannelId,
                PageNumber = data.PageNumber + 1,
            });
            
            paginationButtons.Add(InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Forward, backCallback));
        }
        
        if (paginationButtons.Count > 0)
        {
            buttons.Add(paginationButtons);
        }

        var menuData = _handlersManager.ToCallback(new ShowChannelMenuCallbackData
        {
            ChannelId = data.ChannelId
        });
        
        buttons.Add(new List<InlineKeyboardButton> {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ChannelMenu, menuData)
        });
        
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, MessageTemplate.ChooseZakup,
            replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
    }

    public ChannelStatisticCallbackData Parse(List<string> parameters)
    {
        return new ChannelStatisticCallbackData
        {
            ChannelId = long.Parse(parameters[0]),
            PageNumber = int.Parse(parameters[1]),
        };
    }
}