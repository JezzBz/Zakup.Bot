using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Posts;

[StateType(UserStateType.AddPostTitle)]
public class AddPostTitleHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly AdPostsService _adPostsService;

    public AddPostTitleHandler(UserService userService, AdPostsService adPostsService)
    {
        _userService = userService;
        _adPostsService = adPostsService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var state = await _userService.GetUserState(update.Message!.From!.Id, cancellationToken);
        var data = CacheHelper.ToData<AddPostAliasCache>(state!.CachedValue!);
        var post = await _adPostsService.Get(data!.PostId, cancellationToken);
        var title = update.Message.Text?.Trim() ?? "";
        var isValid = await ValidateMessage(botClient, title, state.UserId, cancellationToken);
        if (!isValid)
        {
            return;
        }

        post.Title = title.ToLowerInvariant();

        await _adPostsService.UpdatePost(post);
        
        await botClient.SafeDelete(update.Message!.From!.Id, update.Message.MessageId, cancellationToken);
        await botClient.SendTextMessageAsync(
            state.UserId,
            $"✅ Креатив *{CommandsHelper.EscapeMarkdownV2(post.Title)}* успешно добавлен\\!\n\nТеперь создайте новый закуп через меню или с помощью inline вызова\\.",
            parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);
        state.Clear();
        await _userService.SetUserState(state, cancellationToken);
    }

    private async Task<bool> ValidateMessage(ITelegramBotClient botClient, string text, long userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            await botClient.SendTextMessageAsync(
                userId,
                MessageTemplate.PostTitleEmptyError, cancellationToken: cancellationToken);
            return false;
        }

        if (text.Length > 30)
        {
            await botClient.SendTextMessageAsync(
                userId,
                MessageTemplate.TitleTooLongError, cancellationToken: cancellationToken);
            return false;
        }

        // Разрешаем латиницу, кириллицу (включая ё), цифры и пробелы
        var regex = new Regex("^[a-zA-Z0-9а-яА-ЯёЁ ]+$");
        if (!regex.IsMatch(text))
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: MessageTemplate.BadSymbolsError, cancellationToken: cancellationToken);
            return false;
        }
        return true;
    }
}