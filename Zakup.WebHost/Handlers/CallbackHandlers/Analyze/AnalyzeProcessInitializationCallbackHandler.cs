using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Analyze;

[CallbackType(CallbackType.AnalyzeInit)]
public class AnalyzeProcessInitializationCallbackHandler : IEmptyCallbackHandler
{
    private readonly UserService _userService;
    private readonly AnalyzeService _analyzeService;

    public AnalyzeProcessInitializationCallbackHandler(UserService userService, AnalyzeService analyzeService)
    {
        _userService = userService;
        _analyzeService = analyzeService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var balance = await _analyzeService.GetAnalyzePoints(callbackQuery.From.Id, cancellationToken);
        if (balance <= 0)
        {
            var keyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.BuyPoints, ((int)CallbackType.AnalyzeBuyPoints).ToString())
            });
            await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,
                MessageTemplate.AnalyzeMenuText(0), replyMarkup:keyboard, cancellationToken: cancellationToken);
            return;
        }
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.State = UserStateType.InitializeSimilarChannelAnalyze;
        state.PreviousMessageId = callbackQuery.Message.MessageId;
        await _userService.SetUserState(state, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId,
            MessageTemplate.AnalyzeChannelStart, cancellationToken: cancellationToken);
    }
}