using System.Text;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Zakup.EntityFramework;

namespace Zakup.WebHost.Jobs
{
    [Quartz.DisallowConcurrentExecution]
    public class DailyReportJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITelegramBotClient _botClient;
        
        // Список исключаемых channelID (с минусом и без)
        private static readonly HashSet<long> ExcludedChannelIds = new HashSet<long>
        {
            -1001604009390,
            1001604009390,
            1001604009390,
            1001949755557,
            1002085914357,
            1001918504251,
            1001911746541,
            1002075953372,
            1001717331382,
            1002201315975,
            1001975319185,
            1002390555756,
            1002399743958,
            1002327754766,
            1002337960205,
            1002142889796,
            1001790694740,
            1001435462526,
            1001720817287,
            1002431585440,
            1002037629068,
            1001672252979,
            1002264429816,
            1001947856224,
            1002066792944,
            1002235429960,
            1002106907647,
            1002155267313,
            1001435747224,
            1002378347273,
            1002358718448,
            1001876618438,
            1002433711383,
            1001269139057,
            1001551764869,
            1002416314170,
            1002056290315,
            1002032201068,
            1002494587355,
            1001916089175,
            1001625740999,
            1001167103913,
            1002303580470,
            1001618905835,
            1002455971509,
            1002042855376,
            1002375539517,
            1002409666993,
            1002487229302,
            1002303948607,
            1002094539447,
            1002308762880,
            1002106475282,
            1001852243980,
            1002103700035,
            1001895648812,
            1001905395305,
            1002315506517,
            1001900276219,
            -1001604009390,
            -1001949755557,
            -1002085914357,
            -1001918504251,
            -1001911746541,
            -1002075953372,
            -1001717331382,
            -1002201315975,
            -1001975319185,
            -1002390555756,
            -1002399743958,
            -1002327754766,
            -1002337960205,
            -1002142889796,
            -1001790694740,
            -1001435462526,
            -1001720817287,
            -1002431585440,
            -1002037629068,
            -1001672252979,
            -1002264429816,
            -1001947856224,
            -1002066792944,
            -1002235429960,
            -1002106907647,
            -1002155267313,
            -1001435747224,
            -1002378347273,
            -1002358718448,
            -1001876618438,
            -1002433711383,
            -1001269139057,
            -1001551764869,
            -1002416314170,
            -1002056290315,
            -1002032201068,
            -1002494587355,
            -1001916089175,
            -1001625740999,
            -1001167103913,
            -1002303580470,
            -1001618905835,
            -1002455971509,
            -1002042855376,
            -1002375539517,
            -1002409666993,
            -1002487229302,
            -1002303948607,
            -1002094539447,
            -1002308762880,
            -1002106475282,
            -1001852243980,
            -1002103700035,
            -1001895648812,
            -1001905395305,
            -1002315506517,
            -1001900276219
        };

        public DailyReportJob(IServiceProvider serviceProvider, ITelegramBotClient botClient)
        {
            _serviceProvider = serviceProvider;
            _botClient = botClient;
        }
        
        /// <summary>
        /// Метод выполнения задания Quartz
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            int totalReportsSent = 0;
            int totalUsersNotified = 0;
            var notifiedUsers = new HashSet<long>();

            try
            {
                Console.WriteLine($"[DailyReport] Запуск отчета {(context == null ? "вручную" : "по расписанию")}");

                // Создаем скоуп для получения необходимых сервисов
                await using var scope = _serviceProvider.CreateAsyncScope();
                var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Вычисляем даты (вчерашний день с 00:00 до 23:59:59 по МСК)
                var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                var nowMoscow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone);
                var yesterdayMoscow = nowMoscow.AddDays(-1).Date;
                var startOfYesterdayUtc = TimeZoneInfo.ConvertTimeToUtc(yesterdayMoscow, moscowTimeZone);
                var endOfYesterdayUtc = startOfYesterdayUtc.AddDays(1).AddTicks(-1);

                Console.WriteLine($"[DailyReport] Запуск отчета за {yesterdayMoscow:yyyy-MM-dd}");

                // День недели и дата для отчета
                string[] daysOfWeek = { "вс", "пн", "вт", "ср", "чт", "пт", "сб" };
                string[] months = { "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };
                string dateStr = $"{daysOfWeek[(int)yesterdayMoscow.DayOfWeek]}, {yesterdayMoscow.Day} {months[yesterdayMoscow.Month - 1]}. {yesterdayMoscow.Year}";

                // Получаем все активные каналы из БД, исключая указанные channelID
                var channels = await database.Channels
                    .Where(c => !c.HasDeleted && !ExcludedChannelIds.Contains(c.Id) && !ExcludedChannelIds.Contains(-c.Id))
                    .ToListAsync();

                Console.WriteLine($"[DailyReport] Найдено {channels.Count} каналов для отчета");

                if (!channels.Any())
                {
                    Console.WriteLine($"[DailyReport] Нет каналов для формирования отчета");
                    return;
                }

                // Для каждого канала формируем отчет
                foreach (var channel in channels)
                {
                    // Получаем список подписок за вчерашний день
                    var joinedYesterday = await database.ChannelMembers
                        .Where(m => m.ChannelId == channel.Id)
                        .Where(m => m.JoinedUtc >= startOfYesterdayUtc && m.JoinedUtc <= endOfYesterdayUtc)
                        .ToListAsync();

                    // Получаем список отписок за вчерашний день
                    var leftYesterday = await database.ChannelMembers
                        .Where(m => m.ChannelId == channel.Id)
                        .Where(m => m.LeftUtc >= startOfYesterdayUtc && m.LeftUtc <= endOfYesterdayUtc)
                        .ToListAsync();

                    // Получаем текущее количество подписчиков
                    var currentSubscribersCount = await database.ChannelMembers
                        .CountAsync(m => m.ChannelId == channel.Id && m.Status && m.LeftUtc == null);

                    // Вычисляем дельту (сколько +/- подписчиков за день)
                    int subscribersDelta = joinedYesterday.Count - leftYesterday.Count;

                    // Группируем подписавшихся по источникам (приглашениям)
                    var joinedBySource = joinedYesterday
                        .GroupBy(m => string.IsNullOrEmpty(m.InviteLinkName) ? "без ссылки" : m.InviteLinkName)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Группируем отписавшихся по источникам
                    var leftBySource = leftYesterday
                        .GroupBy(m => string.IsNullOrEmpty(m.InviteLinkName) ? "без ссылки" : m.InviteLinkName)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Формируем отчет для канала
                    var messageBuilder = new StringBuilder();

                    // Проверяем, есть ли изменения в подписчиках
                    if (joinedYesterday.Count == 0 && leftYesterday.Count == 0)
                    {
                        Console.WriteLine($"[DailyReport] Нет изменений в подписчиках канала '{channel.Title}', отчет не отправляется");
                        continue;
                    }

                    // Заголовок отчета
                    messageBuilder.AppendLine($"За вчера, {dateStr}, у вас {(subscribersDelta >= 0 ? "+" : "")}{subscribersDelta} подписчиков, всего {currentSubscribersCount} в канале «{channel.Title}»");
                    messageBuilder.AppendLine();

                    // Раздел с новыми подписчиками
                    messageBuilder.AppendLine($"🟢 Подписались: {joinedYesterday.Count}");
                    messageBuilder.AppendLine();

                    // Перечисляем подписавшихся по источникам
                    foreach (var source in joinedBySource)
                    {
                        messageBuilder.AppendLine($"({source.Value.Count}) {source.Key}:");

                        foreach (var member in source.Value)
                        {
                            var joinTime = TimeZoneInfo.ConvertTimeFromUtc(member.JoinedUtc ?? DateTime.UtcNow, moscowTimeZone);
                            var isPremium = member.IsPremium == true ? " ★" : "";

                            string userName = string.IsNullOrEmpty(member.UserName)
                                ? "Пользователь"
                                : member.UserName;

                            messageBuilder.AppendLine($"{joinTime:HH:mm} [{userName}](tg://user?id={member.UserId}){isPremium}");
                        }
                        messageBuilder.AppendLine();
                    }

                    // Раздел с отписавшимися
                    if (leftYesterday.Count > 0)
                    {
                        messageBuilder.AppendLine($"🔴 Отписались: {leftYesterday.Count}");
                        messageBuilder.AppendLine();

                        // Перечисляем отписавшихся по источникам
                        foreach (var source in leftBySource)
                        {
                            messageBuilder.AppendLine($"({source.Value.Count}) {source.Key}:");

                            foreach (var member in source.Value)
                            {
                                var leftTime = TimeZoneInfo.ConvertTimeFromUtc(member.LeftUtc ?? DateTime.UtcNow, moscowTimeZone);
                                var isPremium = member.IsPremium == true ? " ★" : "";

                                // Вычисляем, сколько дней пользователь был в канале
                                int daysInChannel = 0;
                                if (member.JoinedUtc.HasValue && member.LeftUtc.HasValue)
                                {
                                    daysInChannel = (int)(member.LeftUtc.Value - member.JoinedUtc.Value).TotalDays;
                                }

                                string userName = string.IsNullOrEmpty(member.UserName)
                                    ? "Пользователь"
                                    : member.UserName;

                                messageBuilder.AppendLine($"{leftTime:HH:mm} [{userName}](tg://user?id={member.UserId}){isPremium} ({daysInChannel} дней)");
                            }
                            messageBuilder.AppendLine();
                        }
                    }

                    // Получаем администраторов канала
                    var admins = await database.ChannelAdministrators
                        .Where(r => r.ChannelId == channel.Id)
                        .Select(r => r.User)
                        .ToListAsync();

                    // Получаем пользователей бота, которые являются администраторами канала
                    var botUsers = await database.Users
                        .Where(u => admins.Select(a => a.Id).Contains(u.Id))
                        .ToListAsync();

                    // Отправляем отчет каждому администратору
                    foreach (var admin in botUsers)
                    {
                        try
                        {
                            await _botClient.SendTextMessageAsync(
                                admin.Id,
                                messageBuilder.ToString(),
                                parseMode: ParseMode.Markdown,
                                disableWebPagePreview: true
                            );

                            totalReportsSent++;
                            if (notifiedUsers.Add(admin.Id))
                            {
                                totalUsersNotified++;
                            }

                            Console.WriteLine($"[DailyReport] Отправлен отчет по каналу '{channel.Title}' пользователю {admin.Id}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DailyReport] Ошибка отправки отчета пользователю {admin.Id}: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"[DailyReport] Итоговая статистика: отправлено {totalReportsSent} отчетов {totalUsersNotified} уникальным пользователям");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DailyReport] Ошибка при выполнении отчета: {ex}");
                throw;
            }
        }
    }
} 