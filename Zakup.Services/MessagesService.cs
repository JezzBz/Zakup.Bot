using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.Services;

public class MessagesService
{
    private readonly UserService _userService;
    private readonly ZakupService _zakupService;
    private readonly ChannelService _channelService;
    public MessagesService(UserService userService, ZakupService zakupService, ChannelService channelService)
    {
        _userService = userService;
        _zakupService = zakupService;
        _channelService = channelService;
    }

    public async Task SendMenu(ITelegramBotClient botClient, TelegramUser user, CancellationToken cancellationToken, int? menuMessageId = null)
    {
        var period = TimeHelper.GetToday();
        var zakupStatistic = await _zakupService.GetStatistics(user.Id, period.StartTime, period.EndTime, cancellationToken);
        var channelsStatistic = await _channelService.GetSubscribeStatistic(user.Id, period.StartTime, period.EndTime, cancellationToken);
        var menuMessage = MessageTemplate.GetMenu(zakupStatistic.Price, zakupStatistic.PaidPrice, channelsStatistic.SubscribeCount, channelsStatistic.SubscribeCount);
        var keyboard = GetKeyboard(user.Id);
        user.UserState.State = UserStateType.None;
        if (menuMessageId != null)
        {
            await botClient.SafeEdit(user.Id, menuMessageId.Value, menuMessage,
                parseMode: ParseMode.Markdown, replyMarkup: new InlineKeyboardMarkup(keyboard),
                cancellationToken: cancellationToken);
            
            user.UserState.MenuMessageId = menuMessageId.Value;
            
        }
        else
        { 
            user.UserState.MenuMessageId = (await botClient.SendTextMessageAsync(user.Id, menuMessage, parseMode:ParseMode.Markdown, replyMarkup: new InlineKeyboardMarkup(keyboard), cancellationToken: cancellationToken)).MessageId;
        }
        
        await _userService.SetUserState(user.Id, user.UserState, cancellationToken);
    }


    private List<List<InlineKeyboardButton>> GetKeyboard(long userId)
    {
        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ZakupCreate, ((int)CallbackType.ZakupCreate).ToString()),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.MyChannels, ((int)CallbackType.MyChannelsList).ToString()),
            },
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.GoogleSheets, ((int)CallbackType.GoogleSheets).ToString()),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Help, ((int)CallbackType.Help).ToString())
            }
        };

        if (_userService.IsAdmin(userId))
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.AdminPanel, ((int)CallbackType.AdminPanel).ToString())
            });
        }

        return keyboard;
    }
}