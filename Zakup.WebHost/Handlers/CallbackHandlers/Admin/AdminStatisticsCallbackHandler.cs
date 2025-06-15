using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminStatistics)]
public class AdminStatisticsCallbackHandler : ICallbackHandler<AdminStatisticsCallbackData>
{
    private readonly StatisticsService _statisticsService;

    public AdminStatisticsCallbackHandler(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminStatisticsCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var statistics = await _statisticsService.GetDailyStatistics(cancellationToken);
        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            statistics,
            cancellationToken: cancellationToken);
    }
} 