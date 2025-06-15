using System.Globalization;
using System.Text;
using Bot.Core;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers;

/// <summary>
/// События без контекста
/// </summary>
public class StatelessHandler : IUpdatesHandler
{
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;
    private readonly UserService _userService;

    public StatelessHandler(ChannelService channelService, HandlersManager handlersManager, UserService userService)
    {
        _channelService = channelService;
        _handlersManager = handlersManager;
        _userService = userService;
    }
    
    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.ForwardFromChat != null)
        {
            await ChannelRating(botClient,update,cancellationToken);
            return;
        }
    
        if (update.Message?.ForwardFrom != null && !update.Message.ForwardFrom.IsBot)
        {
            await PrintUserInfo(botClient, update.Message.From!.Id,update.Message?.ForwardFrom!, cancellationToken);
        }
        
        //Информация по @нику
        if (( (update.Message?.Text?.StartsWith("@") ?? false) &&
             CommandsHelper.WordsCount(update.Message?.Text) == 1))
        {
            var username = update.Message!.Text!.Replace("@","");
            var member = await _userService.GetMemberByUsername(username);
            if (member == null)
            {
                await botClient.SendTextMessageAsync(update.Message.From!.Id,
                    MessageTemplate.MemberNotFound , cancellationToken: cancellationToken);
                return;
            }
            var memberInfo = await botClient.GetChatMemberAsync(member.ChannelId, member.UserId, cancellationToken: cancellationToken);
            await PrintUserInfo(botClient, update.Message.From!.Id, memberInfo.User, cancellationToken);
        }
    }
    
    
    private async Task PrintUserInfo(ITelegramBotClient botClient, long requesterId, User user, CancellationToken cancellationToken)
     {
         var messageBuilder = new StringBuilder();

         var resultQuery = await _userService.GetUserInfo(botClient, user.Id, requesterId, user.IsPremium ?? false,
             user.Username!, cancellationToken);

         await _userService.UpdateUntrackedMemberChanges(botClient, requesterId, user.Id, cancellationToken: cancellationToken);
         await resultQuery.ForEachAsync(member =>
         {
             messageBuilder.AppendLine(
                 $"Пользователь вступил в {member.Member.Channel.Title} - {member.Member.JoinedUtc?.AddHours(3).ToString(CultureInfo.InvariantCulture) ?? "Неизвестно"}");
  
             _ = member.Member.InviteLink is null
                 ? messageBuilder.AppendLine("Изначальная аудитория")
                 : messageBuilder.AppendLine($"По ссылке: {member.Member.InviteLink}");
  
             _ = member.Zakup is null
                 ? messageBuilder.AppendLine("Площадка: Неизвестно")
                 : messageBuilder.AppendLine($"Площадка: {member.Zakup.Platform}");
  
             if (member.Zakup is not null)
             {
                 messageBuilder.AppendLine($"Цена: {member.Zakup.Price}");
             }
  
             messageBuilder.AppendLine("");
         }, cancellationToken: cancellationToken);
  
         var responseText = messageBuilder.ToString();
         if (responseText.Length == 0)
         {
             await botClient.SendTextMessageAsync(requesterId,
                 MessageTemplate.MemberNotFound, cancellationToken: cancellationToken);
             return;
         }

         var callbackData =  await _handlersManager.ToCallback(new MarkAsLeadCallbackData
         {
             LeadUserId = user.Id
         });
         var buttons = new List<InlineKeyboardButton>()
         {
             InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.MarkAsLead, callbackData)
         };
  
         await botClient.SendTextMessageAsync(requesterId, responseText,
             replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
     }

    private async Task ChannelRating(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var channelId = update.Message!.ForwardFromChat!.Id;
        var rating = await _channelService.GetRating(channelId, cancellationToken);
        if (rating == null)
        {
            rating = new ChannelRating()
            {
                ChannelId = channelId,
                BadDeals = 0,
                Rate = 0
            };
            await _channelService.AddRating(rating,cancellationToken);
        }
        var messageBuilder = new StringBuilder();
        if (rating.BadDeals > 0)
        {
            messageBuilder.AppendLine("ОСТОРОЖНО, МОШЕННИКИ!");
        }
        else if (rating.Rate < -5)
        {
            messageBuilder.AppendLine("ВНИМАНИЕ! Негативный рейтинг канала");
        }
        else
        {
            messageBuilder.AppendLine("Жалоб на канал не поступало");
        }
        var positiveFeedback = await _channelService.GetPositiveFeedbackCount(channelId, cancellationToken);
        var negativeFeedback = await _channelService.GetNegativeFeedbackCount(channelId, cancellationToken);
        messageBuilder.AppendLine(
            $"\ud83c\udfc5 Репутация: ({rating.BadDeals})\u26d4 ({negativeFeedback})\ud83d\udc4e ({positiveFeedback})\ud83d\udc4d");

        var positiveData = await _handlersManager.ToCallback(new RateChannelCallbackData
        {
            RateType = ChannelRateType.Like,
            ChannelId = channelId
        });
        
        var negativeData = await _handlersManager.ToCallback(new RateChannelCallbackData
        {
            RateType = ChannelRateType.Dislike,
            ChannelId = channelId
        });
        
        var reportData = await _handlersManager.ToCallback(new RateChannelCallbackData
        {
            RateType = ChannelRateType.Report,
            ChannelId = channelId
        });
        
        var keyBoard = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData("\u26d4 Пожаловаться", reportData),
            InlineKeyboardButton.WithCallbackData("\ud83d\udc4d Доволен рекламой", positiveData),
            InlineKeyboardButton.WithCallbackData("\ud83d\udc4e Не доволен", negativeData),
        };
        
        await botClient.SendTextMessageAsync(update.Message.From!.Id, messageBuilder.ToString(),
            replyMarkup: new InlineKeyboardMarkup(keyBoard), cancellationToken: cancellationToken);
    }
    
    
    public static bool ShouldHandle(Update update)
    {
        throw new Exception();
    }
}