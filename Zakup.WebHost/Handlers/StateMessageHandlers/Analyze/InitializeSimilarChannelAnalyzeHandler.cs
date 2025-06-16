using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.StateMessageHandlers.Analyze;

[StateType(UserStateType.InitializeSimilarChannelAnalyze)]
public class InitializeSimilarChannelAnalyzeHandler : IStateHandler
{
    private readonly AnalyzeService _analyzeService;
    private readonly UserService _userService;

    public InitializeSimilarChannelAnalyzeHandler(AnalyzeService analyzeService, UserService userService)
    {
        _analyzeService = analyzeService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(update.Message?.Text) ||!CommandsHelper.IsNickname(update.Message?.Text ?? ""))
        {
            await botClient.SafeEdit(update.Message!.From!.Id, update.Message.MessageId, MessageTemplate.BadChannelNickname, cancellationToken: cancellationToken);
            return;
        }
        
        var message = update.Message;
        var channel = message!.Text;
        
        var balance = await _analyzeService.GetAnalyzePoints(message.From.Id, cancellationToken);
        var state = await _userService.GetUserState(message.From.Id, cancellationToken);
        if (balance <= 0)
        {
            var keyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.BuyPoints,
                    ((int)CallbackType.AnalyzeBuyPoints).ToString())
            });
            
            await botClient.SafeEdit(message.From.Id, state.PreviousMessageId, MessageTemplate.AnalyzeBalanceError, replyMarkup:keyboard, cancellationToken: cancellationToken);
            return;
        }
        var id = await _analyzeService.Analyze(message.From.Id, channel, cancellationToken);
        await botClient.SafeEdit(message.From.Id, state!.PreviousMessageId, MessageTemplate.AnalyzeProcessStarted(id), cancellationToken: cancellationToken);
    }
    
    
}