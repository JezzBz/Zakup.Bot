using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.AdminInfo)]
public class ChannelAdminInfoCallbackHandler : ICallbackHandler<AdminInfoCallbackData>
{
    private readonly UserService _userService;
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ChannelAdminInfoCallbackHandler(UserService userService, ZakupService zakupService, HandlersManager handlersManager)
    {
        _userService = userService;
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminInfoCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(data.UserId, cancellationToken);
        if (user == null)
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.AdminNotFound, cancellationToken: cancellationToken);
            return;
        }
        var adminDisplay = string.IsNullOrEmpty(user.UserName) ? user.Id.ToString() : "@" + user.UserName;
        var zakups = await _zakupService.GetAdminZakups(adminDisplay, data.ChannelId, cancellationToken);
        var totalSum = zakups.Sum(z => z.Price);
        var averagePrice = zakups.Any() ? zakups.Average(z => z.Price) : 0;
        
       
        var addedDate = "нет данных"; 
        var messageText = MessageTemplate.AdminInfo(adminDisplay, totalSum, averagePrice, addedDate,zakups.Count);
        
        var removeAdminData = await _handlersManager.ToCallback(new RemoveAdminCallbackData
        {
            ChannelId = data.ChannelId,
            AdminUserId = data.UserId
        });
        var backData = await _handlersManager.ToCallback(new ChannelAdminsListCallbackData
        {
            ChannelId = data.ChannelId
        });
        
        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Delete, removeAdminData)
            },
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backData)
            }
        };
        
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, messageText, replyMarkup: new InlineKeyboardMarkup(keyboard), cancellationToken: cancellationToken);
    }

    public AdminInfoCallbackData Parse(List<string> parameters)
    {

        return new AdminInfoCallbackData
        {
            UserId = long.Parse(parameters[0]),
            ChannelId = long.Parse(parameters[1])
        };
    }
}