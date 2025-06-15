using Microsoft.EntityFrameworkCore;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Zakup.EntityFramework;

namespace Zakup.WebHost.Jobs
{
    [DisallowConcurrentExecution] // Пункт 1: Запрещаем параллельные выполнения одного и того же Job
    public class RequestsApproveJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITelegramBotClient _botClient;

        public RequestsApproveJob(IServiceProvider serviceProvider, ITelegramBotClient botClient)
        {
            _serviceProvider = serviceProvider;
            _botClient = botClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Пункт 3: добавляем условие r.ApprovedUtc == null, чтобы не трогать уже одобренные
            var requests = await ctx.ChannelJoinRequests
                .Where(r => r.ApprovedUtc == null)
                .Where(r => r.Channel.MinutesToAcceptRequest != null)
                .Where(r => r.RequestedUtc != null)
                .Where(r => r.RequestedUtc.HasValue
                            && r.Channel.MinutesToAcceptRequest.HasValue
                            && r.RequestedUtc.Value.AddMinutes(r.Channel.MinutesToAcceptRequest.Value) 
                               <= DateTime.UtcNow)
                .Take(100)
                .ToListAsync();

            foreach (var r in requests)
            {
                try
                {
                    await _botClient.ApproveChatJoinRequest(r.ChannelId, r.UserId);
                }
                catch (ApiRequestException e)
                {
                    if (!e.Message.Contains("USER_ALREADY_PARTICIPANT")
                        && !e.Message.Contains("HIDE_REQUESTER_MISSING")
                        && !e.Message.Contains("USER_CHANNELS_TOO_MUCH")
                        && !e.Message.Contains("user is deactivated")
                        && !e.Message.Contains("chat not found")
                        && !e.Message.Contains("bot is not a member of the channel chat"))
                    {
                        throw;
                    }
                }

                r.ApprovedUtc = DateTime.UtcNow;
            }

            ctx.UpdateRange(requests);
            await ctx.SaveChangesAsync();
        }
    }

    public class DisallowConcurrentExecutionAttribute : Attribute
    {
    }
}
