using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

/// <summary>
/// Обработчик команды "Оценка канала"
/// </summary>
[CallbackType(CallbackType.RateChannel)]
public class RateChannelCallBackHandler : ICallbackHandler<RateChannelCallbackData>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public RateChannelCallBackHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public RateChannelCallbackData Parse(List<string> parameters)
    {
        return new RateChannelCallbackData
        {
            RateType = Enum.Parse<ChannelRateType>(parameters[0]),
            ChannelId = long.Parse(parameters[1])
        };
    }
    
    public async Task Handle(ITelegramBotClient botClient, RateChannelCallbackData data, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await (data.RateType switch
        {
            ChannelRateType.Report => HandleReport(botClient, callbackQuery.From.Id),
            ChannelRateType.Like => HandleRate(botClient, data.ChannelId,  callbackQuery.From.Id, true),
            ChannelRateType.Dislike => HandleRate(botClient, data.ChannelId,  callbackQuery.From.Id, false), 
                _ => throw new ArgumentOutOfRangeException()
        });
    }
    
    private async Task HandleReport(ITelegramBotClient botClient, long userId)
    {
        await botClient.SendTextMessageAsync(userId,
            "Пожалуйста пришлите доказательства и ссылку на канал(или перешлите пост из него) в личные сообщения @gandalftg");
    }
    
    private async Task HandleRate(ITelegramBotClient botClient, long channelId, long userId, bool positive)      
    {
        var rateValue = positive ? 1 : -1;
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await AssertNoMute(botClient, context, userId);
        
        var channelRating = await context.ChannelRatings.FirstOrDefaultAsync(c => c.ChannelId == channelId);
        
        if (channelRating is null)
        {
            channelRating = new ChannelRating()
            {
                ChannelId = channelId,
                BadDeals = 0,
                Rate = 0
            };
            await context.AddAsync(channelRating);
        }
        
        var previousFeedBack = await context.ChannelFeedback
            .Where(f => f.FromUserId == userId)
            .FirstOrDefaultAsync(f => f.ChannelId == channelId);

        if (previousFeedBack is null) //Это первый фидбек
        {
            await context.AddAsync(new ChannelFeedback()
            {
                FromUserId = userId,
                ChannelId = channelId,
                Positive = positive,
                CreatedUtc = DateTime.UtcNow
            });

            channelRating.Rate += rateValue;
        }
        else if (previousFeedBack.Positive) //Предыдущая оценка Like
        {
            if (positive) //Текущая оценка like
            {
                await botClient.SendTextMessageAsync(userId, "⚠️ Вы уже повысили рейтинг этого канала!");
                return;
            }
            channelRating.Rate += rateValue * 2;
            previousFeedBack.Positive = positive;
        }
        else //Предыдущая оценка dislike
        {
            if (!positive) //Текущая оценка dislike
            {
                await botClient.SendTextMessageAsync(userId, "⚠️ Вы уже понизили рейтинг этого канала!");
                return;
            }
            channelRating.Rate += rateValue * 2;
            
        }
        
        await context.SaveChangesAsync();

        var stringStatus = positive ? "повышен" : "понижен";
        await botClient.SendTextMessageAsync(userId,
            $"Рейтинг канала {stringStatus}!");
        
        await SendFeedbackInfoToOwner(context, botClient, channelId,false, userId);
    }

    private async Task AssertNoMute(ITelegramBotClient botClient, ApplicationDbContext context, long userId)
    {
        var user = await context.Users.FirstAsync(u => u.Id == userId);
        
        if (user.MutedToUtc > DateTime.UtcNow)
        {
            await botClient.SendTextMessageAsync(chatId: userId,
                text: "⏳ *Вы слишком часто отправляете оценку.* Попробуйте позже.",
                parseMode: ParseMode.MarkdownV2
            );
            return;
        }
    }
    
    private async Task SendFeedbackInfoToOwner(ApplicationDbContext context, ITelegramBotClient botClient,  long channelId, bool isUpRating, long userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(c => c.Id == userId);
        var likeOrDislike = isUpRating ? "повысил":"понизил";
        var message = $"Пользователь [{user.UserName}|{user.Id}] {likeOrDislike} рейтинг канала {channelId}";
        
        await botClient.SendTextMessageAsync(353167378, message);
    }
}