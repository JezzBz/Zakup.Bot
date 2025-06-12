using System.Text;
using Microsoft.EntityFrameworkCore;
using Zakup.Abstractions.DataContext;
using Zakup.Common.Enums;
using Zakup.EntityFramework;

namespace Zakup.Services;

public class StatisticsService
{
    private readonly ApplicationDbContext _context;

    public StatisticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetDailyStatistics(CancellationToken cancellationToken)
    {
        var messageBuilder = new StringBuilder("Статистика за сегодня:");

        // Общая статистика
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        messageBuilder.AppendLine($"Общее количество пользователей: {totalUsers}");

        var totalChannels = await _context.Channels.CountAsync(cancellationToken);
        messageBuilder.AppendLine($"Общее количество каналов: {totalChannels}");
        messageBuilder.AppendLine();

        // Статистика закупов
        var zakupStat = _context.TelegramZakups
            .Where(z => z.CreatedUtc.Day == DateTime.UtcNow.Day)
            .Select(z => new { Source = z.ZakupSource, Price = z.Price, Paid = z.IsPad });

        if (await zakupStat.AnyAsync(cancellationToken))
        {
            messageBuilder.AppendLine($"Закупов создано через бота: {await zakupStat.Where(s => s.Source == ZakupSource.Bot).CountAsync(cancellationToken)}");
            messageBuilder.AppendLine($"На сумму: {await zakupStat.Where(s => s.Source == ZakupSource.Bot).SumAsync(s => s.Price, cancellationToken)}");
            messageBuilder.AppendLine($"Оплачено из них: {await zakupStat.Where(s => s.Source == ZakupSource.Bot && s.Paid).CountAsync(cancellationToken)}");
            messageBuilder.AppendLine($"На сумму: {await zakupStat.Where(s => s.Source == ZakupSource.Bot && s.Paid).SumAsync(s => s.Price, cancellationToken)}");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Закупов создано через inline: {await zakupStat.Where(s => s.Source == ZakupSource.Inline).CountAsync(cancellationToken)}");
            messageBuilder.AppendLine($"На сумму: {await zakupStat.Where(s => s.Source == ZakupSource.Inline).SumAsync(s => s.Price, cancellationToken)}");
            messageBuilder.AppendLine($"Оплачено из них: {await zakupStat.Where(s => s.Source == ZakupSource.Inline && s.Paid).CountAsync(cancellationToken)}");
            messageBuilder.AppendLine($"На сумму: {await zakupStat.Where(s => s.Source == ZakupSource.Inline && s.Paid).SumAsync(s => s.Price, cancellationToken)}");
            messageBuilder.AppendLine();
        }
        else
        {
            messageBuilder.AppendLine("Закупы отсутствуют");
        }

        // Статистика фидбеков
        var feedBackStat = _context.ChannelFeedback
            .Where(f => f.CreatedUtc.Day == DateTime.UtcNow.Day);

        if (await feedBackStat.AnyAsync(cancellationToken))
        {
            messageBuilder.AppendLine($"Фидбеков о каналах: {await feedBackStat.CountAsync(cancellationToken)}");
            messageBuilder.AppendLine($"Положительных: {await feedBackStat.Where(f => f.Positive).CountAsync(cancellationToken)}");
            messageBuilder.AppendLine($"Отрицательных: {await feedBackStat.Where(f => !f.Positive).CountAsync(cancellationToken)}");
            messageBuilder.AppendLine();
        }
        else
        {
            messageBuilder.AppendLine("Оценки отсутствуют");
        }

        // Статистика пересланных сообщений
        var forwardsStat = _context.MessageForwards
            .Where(f => f.ForwardAtUtc.Day == DateTime.UtcNow.Day);

        if (await forwardsStat.AnyAsync(cancellationToken))
        {
            messageBuilder.AppendLine($"Сообщений переслано от пользователей: {await forwardsStat.Where(f => f.Source == MessageForwardSource.User).CountAsync(cancellationToken)}");
            messageBuilder.AppendLine($"Сообщений переслано от каналов: {await forwardsStat.Where(f => f.Source == MessageForwardSource.Channel).CountAsync(cancellationToken)}");
        }
        else
        {
            messageBuilder.AppendLine("Пересланные сообщения отсутствуют");
        }

        return messageBuilder.ToString();
    }
} 