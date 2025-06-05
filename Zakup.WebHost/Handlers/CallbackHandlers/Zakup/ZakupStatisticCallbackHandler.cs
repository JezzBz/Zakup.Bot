using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ZakupStatistic)]
public class ZakupStatisticCallbackHandler : ICallbackHandler<ZakupStatisticCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ZakupStatisticCallbackHandler(ZakupService zakupService, HandlersManager handlersManager)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ZakupStatisticCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var placementsData = await _zakupService.GetPlacementData(data.ZakupId, cancellationToken);
        if (placementsData == null)
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.ZakupNotFound, cancellationToken: cancellationToken);
            return;
        }
        
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,
            MessageTemplate.ZakupStatisticMessage(placementsData), 
            parseMode: ParseMode.MarkdownV2, 
            replyMarkup: await GetKeyboard(data, placementsData),
            cancellationToken: cancellationToken);
    }

    private async Task<InlineKeyboardMarkup> GetKeyboard(ZakupStatisticCallbackData data, PlacementStatisticDTO placementsData)
    {
        var showMembersData = await _handlersManager.ToCallback(new ShowZakupMembersCallbackData
        {
            ZakupId = data.ZakupId
        });
        
        var checkSubscribersData = await _handlersManager.ToCallback(new CheckSubscribersCallbackData
        {
            ZakupId = data.ZakupId
        });
        
        var deleteData = await _handlersManager.ToCallback(new DeleteZakupCallbackData()
        {
            ZakupId = data.ZakupId
        });
        
        var editPriceData = await _handlersManager.ToCallback(new ChangePriceCallbackData()
        {
            ZakupId = data.ZakupId
        });
        
        var backData = await _handlersManager.ToCallback(new ChannelStatisticCallbackData()
        {
            ChannelId = placementsData.ChannelId,
            PageNumber = 1
        });
        
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ShowMembers, showMembersData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.CheckSubscribers, checkSubscribersData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Delete, deleteData),
            },
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ChangePrice, editPriceData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backData)
            }
        };
        return new InlineKeyboardMarkup(buttons);
    }
    public ZakupStatisticCallbackData Parse(List<string> parameters)
    {
        return new ZakupStatisticCallbackData
        {
            ZakupId = Guid.Parse(parameters[0])
        };
    }
}