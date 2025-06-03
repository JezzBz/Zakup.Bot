using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ShowChannelMenu)]
public class ShowChannelMenuCallbackHandler : ICallbackHandler<ShowChannelMenuCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;
    
    public ShowChannelMenuCallbackHandler(ChannelService channelService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _handlersManager = handlersManager;
    }

    public  async Task Handle(ITelegramBotClient botClient, ShowChannelMenuCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
       var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
       
       if (channel == null) return; // Не нашли канал, можно добавить сообщение об ошибке
       var keyboard = await GetKeyboard(channel, cancellationToken);
       await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, $"Канал {channel.Title} [{channel.Alias}]", replyMarkup: keyboard, cancellationToken: cancellationToken);
    }


    private async Task<InlineKeyboardMarkup> GetKeyboard(TelegramChannel channel, CancellationToken cancellationToken)
    {
        var pendingRequestsCount = await _channelService.GetPendingRequestsCount(channel.Id, cancellationToken);

        var creaButtonData = _handlersManager.ToCallback(new AdPostListCallbackData
        {
            ChannelId = channel.Id
        });
        var statisticButtonData = _handlersManager.ToCallback(new ChannelStatisticCallbackData
        {
            ChannelId = channel.Id,
            PageNumber = 1
        });
        var autoApproveButtonData = _handlersManager.ToCallback(new ChannelAutoApproveCallbackData
        {
            ChannelId = channel.Id
        });

        var changeLabelButtonData = _handlersManager.ToCallback(new ChangeChannelLabelCallbackData
        {
            ChannelId = channel.Id
        });
        
        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.AdPosts, creaButtonData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Statistic, statisticButtonData)
            },
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.AutoApproveRequests, autoApproveButtonData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ChangeLabel, changeLabelButtonData)
            },
        };
        
        var addAdminButtonData = _handlersManager.ToCallback(new ChannelAdminsListCallbackData()
        {
            ChannelId = channel.Id
        });
        if (channel.ChannelChatId is null)
        {
            var addChatButtonData = _handlersManager.ToCallback(new AddChannelChatCallbackData()
            {
                ChannelId = channel.Id
            });
            
           
            // Кнопки "Отслеживать комментарии" и "Администраторы" на одной строке
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.TrackComments,
                    addChatButtonData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Administrators,
                    addAdminButtonData)
            });
        }
        else
        {
            // Только кнопка "Администраторы" на отдельной строке
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Administrators,
                    addAdminButtonData)
            });
        }

        // Добавляем кнопку "✅ Принять все заявки", если есть ожидающие заявки
        if (pendingRequestsCount > 0)
        {
            var approveButtonData = _handlersManager.ToCallback(new ApproveAllChannelRequestsCallbackData()
            {
                ChannelId = channel.Id
            });
            
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"{ButtonsTextTemplate.AcceptAllRequests} ({pendingRequestsCount})", approveButtonData)
            });
        }
        
        var removeButtonData = _handlersManager.ToCallback(new DeleteChannelCallbackData()
        {
            ChannelId = channel.Id
        });
        
        keyboard.Add([
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Delete, removeButtonData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, nameof(CallbackType.MyChannelsList))
        ]);

        return new InlineKeyboardMarkup(keyboard);
    }
    
    public ShowChannelMenuCallbackData Parse(List<string> parameters)
    {
        return new ShowChannelMenuCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}