using System.Text;
using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers;

/// <summary>
/// События без контекста
/// </summary>
public class StatelessHandler : IUpdatesHandler
{
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;

    public StatelessHandler(ChannelService channelService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _handlersManager = handlersManager;
    }

    public static bool ShouldHandle(Update update)
    {
        return false;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.ForwardFromChat != null)
        {
            await ChannelRating(botClient,update,cancellationToken);
            return;
        }
    }
    //TODO: отрефакторить и добавить
  //    private async Task PrintUserInfo(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
  //   {
  //       await using var scope = _provider.CreateAsyncScope();
  //       var _dataContext = scope.ServiceProvider.GetRequiredService<AppDatabase>();
  //
  //       var message = update.Message;
  //       var messageBuilder = new StringBuilder();
  //
  //       var memberInfoQuery = _dataContext.TelegramChannel_Members
  //           .Include(m => m.Channel)
  //           .Where(m => m.UserId == message.ForwardFrom.Id && m.Channel.Administrators.Any(a => a.Id == message.From.Id));
  //           // .Where(m => m.UserId == message.ForwardFrom!.Id)
  //           // // Добавляем условие, что текущий пользователь является администратором этого канала
  //           // .Where(m => m.Channel.Administrators.Any(a => a.Id == message.From!.Id));
  //       var forwardMessage = new MessageForward
  //       {
  //           UserId = message!.From!.Id,
  //           ForwardAtUtc = DateTime.UtcNow,
  //           Source = MessageForwardSource.User
  //       };
  //       await _dataContext.AddAsync(forwardMessage, cancellationToken);
  //       await _dataContext.SaveChangesAsync(cancellationToken);
  //       var resultQuery = from m in memberInfoQuery
  //           join z in _dataContext.ZakupEntities on m.InviteLink equals z.InviteLink into outter
  //           from o in outter.DefaultIfEmpty()
  //           select new { Zakup = o, Member = m };
  //
  //       var untrackedChannelsMember = await _dataContext.TelegramChannels
  //           .Where(tc => tc.Administrators.Any(c => c.Id == message.From!.Id))
  //           .Where(tc => tc.Members.All(m => m.UserId != message.ForwardFrom!.Id))
  //           .ToListAsync(cancellationToken: cancellationToken);
  //
  //       var untrackedMemberList = new List<TelegramChannel_Member>();
  //
  //       foreach (var c in untrackedChannelsMember)
		// {
		// 	try
		// 	{
  //               // Console.WriteLine(с.Id);
		// 		var memberInfo = await botClient.GetChatMemberAsync(c.Id, message.ForwardFrom!.Id, cancellationToken: cancellationToken);
  //
		// 		if (memberInfo.Status is ChatMemberStatus.Kicked or ChatMemberStatus.Left)
		// 		{
		// 			continue;
		// 		}
  //
		// 		var newMember = new TelegramChannel_Member
		// 		{
		// 			UserId = message.ForwardFrom!.Id,
		// 			IsPremium = message.ForwardFrom.IsPremium,
		// 			UserName = message.ForwardFrom.Username,
		// 			ChannelId = c.Id,
		// 			Status = true,
		// 			Refer = "origin",
		// 			JoinCount = 1,
		// 		};
  //
		// 		untrackedMemberList.Add(newMember);
		// 	}
		// 	catch (ApiRequestException ex) when (ex.Message.Contains("bot is not a member of the channel chat") || ex.Message.Contains("chat not found"))
		// 	{
		// 		// Optionally log the error or inform the user
		// 		Console.WriteLine($"Ошика Bot is not an admin in the channel {c.Id}. Cannot retrieve member info.");
		// 		continue;
		// 	}
		// }
  //
  //
  //       await _dataContext.AddRangeAsync(untrackedMemberList, cancellationToken);
  //       await _dataContext.SaveChangesAsync(cancellationToken);
  //
  //       await resultQuery.ForEachAsync(member =>
  //       {
  //           messageBuilder.AppendLine(
  //               $"Пользователь вступил в {member.Member.Channel.Title} - {member.Member.JoinedUtc?.AddHours(3).ToString(CultureInfo.InvariantCulture) ?? "Неизвестно"}");
  //
  //           _ = member.Member.InviteLink is null
  //               ? messageBuilder.AppendLine("Изначальная аудитория")
  //               : messageBuilder.AppendLine($"По ссылке: {member.Member.InviteLink}");
  //
  //           _ = member.Zakup is null
  //               ? messageBuilder.AppendLine("Площадка: Неизвестно")
  //               : messageBuilder.AppendLine($"Площадка: {member.Zakup.Platform}");
  //
  //           if (member.Zakup is not null)
  //           {
  //               messageBuilder.AppendLine($"Цена: {member.Zakup.Price}");
  //           }
  //
  //           messageBuilder.AppendLine("");
  //       }, cancellationToken: cancellationToken);
  //
  //       var responseText = messageBuilder.ToString();
  //       if (responseText.Length == 0)
  //       {
  //           await botClient.SendTextMessageAsync(message.Chat.Id,
  //               "Пользователь не обнаржуен ни в одном из ваших каналов", cancellationToken: cancellationToken);
  //           return;
  //       }
  //
  //       var buttons = new List<InlineKeyboardButton>()
  //       {
  //           InlineKeyboardButton.WithCallbackData("Отметить как лид", $"lead_userId:{message.ForwardFrom!.Id}")
  //       };
  //
  //       await botClient.SendTextMessageAsync(message.Chat.Id, responseText,
  //           replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
  //   }

    public async Task ChannelRating(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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

        var positiveData = _handlersManager.ToCallback(new RateChannelCallbackData
        {
            RateType = ChannelRateType.Like,
            ChannelId = channelId
        });
        
        var negativeData = _handlersManager.ToCallback(new RateChannelCallbackData
        {
            RateType = ChannelRateType.Dislike,
            ChannelId = channelId
        });
        
        var reportData = _handlersManager.ToCallback(new RateChannelCallbackData
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
}