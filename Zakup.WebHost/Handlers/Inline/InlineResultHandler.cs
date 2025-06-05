using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Bot.Core;
using Google;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services;
using Zakup.Services.Extensions;

namespace Zakup.WebHost.Handlers.Inline;

public class InlineResultHandler : IUpdatesHandler
{
    private readonly MetadataStorage _metadataStorage;
    //private readonly InternalSheetsService _sheetsService;
    private readonly ILogger<InlineResultHandler> _logger;
    private readonly ApplicationDbContext _dataContext;

    public InlineResultHandler(MetadataStorage metadataStorage, ILogger<InlineResultHandler> logger, ApplicationDbContext dataContext)
    {
        _metadataStorage = metadataStorage;
        _logger = logger;
        _dataContext = dataContext;
    }

    public static bool ShouldHandle(Update update)
    {
        return update.Type == UpdateType.ChosenInlineResult;
    }

    public class ResponseItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("participants_count")]
        public string ParticipantsCount { get; set; }

        [JsonPropertyName("ava")]
        public string Ava { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("analytics_link")]
        public string AnalyticsLink { get; set; }

        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; }

        [JsonPropertyName("badlist")]
        public string Badlist { get; set; }

        [JsonPropertyName("typed")]
        public string Typed { get; set; }
    }


    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
       
        var data = update.ChosenInlineResult!;

        if (string.IsNullOrEmpty(data.InlineMessageId))
            throw new InvalidOperationException();

        if (!Guid.TryParse(data.ResultId, out var adPostId))
            throw new InvalidOperationException();

        var text = _metadataStorage.PostMetadataStorage[data.From.Id];
        var callBackInfo = text.Split(" ");

        if (callBackInfo.Length < 3)
            throw new InvalidOperationException("Недостаточно информации для обработки запроса.");

        string platform = "", alias = "", adPostTitle = "", admin = "";
        TelegramAdPost adPost;
        decimal price;

        if (!decimal.TryParse(callBackInfo[^2], out price))
        {
            if (!decimal.TryParse(callBackInfo[^3], out price))
            {
                throw new InvalidOperationException("Некорректный формат цены.");
            }

            alias = callBackInfo[^2];
            adPostTitle = callBackInfo[^1];
            platform = string.Join(" ", callBackInfo.Take(callBackInfo.Length - 3));
        }
        else
        {
            alias = callBackInfo[^1];
            platform = string.Join(" ", callBackInfo.Take(callBackInfo.Length - 2));
        }

        adPost = await _dataContext.TelegramAdPosts
                .Include(a => a.Channel)
                    .ThenInclude(c => c.Administrators)
            
                .FirstAsync(a => a.Id == adPostId, cancellationToken: cancellationToken);

        _metadataStorage.PostMetadataStorage.TryRemove(data.From.Id, out var resultQuery);

        // ... existing code ...
        Telegram.Bot.Types.ChatInviteLink? link = null;
        try
        {
            // Проверяем ID пользователя и модифицируем resultQuery при необходимости
            string queryToUse = resultQuery ?? "";
            if ((data.From.Id == 6159930137 || data.From.Id == 1277718409 || data.From.Id == 6418126337) && !string.IsNullOrEmpty(queryToUse))
            {
                // Убираем число в конце запроса (цену)
                string[] parts = queryToUse.Split(' ');
                if (parts.Length > 0 && decimal.TryParse(parts[^2], out _))
                {
                    queryToUse = $" {platform}";
                }
            }
            
            link = await botClient.ReplaceInviteLink(adPost, queryToUse, data.From.Id);
        }
		catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("bot is not a member"))
		{
			_logger.LogError(ex, "Ошибка: бот не является участником канала {ChannelId}.", adPost.ChannelId);
			await botClient.SendTextMessageAsync(data.From.Id, "Ошибка создания размещения: бот не является участником канала. Добавьте его в канал и повторите попытку.", cancellationToken: cancellationToken);
			return;
		}
		catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("chat not found"))
		{
			_logger.LogError(ex, "Ошибка: канал для создания размещения не найден.", adPost.ChannelId);
			await botClient.SendTextMessageAsync(data.From.Id, "Ошибка создания размещения: канал не найден. Убедитесь, что канал существует и бот имеет к нему доступ.", cancellationToken: cancellationToken);
			return;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Неожиданная ошибка при попытке заменить пригласительную ссылку для канала {ChannelId}.", adPost.ChannelId);
			await botClient.SendTextMessageAsync(data.From.Id, "Произошла неожиданная ошибка при создании размещения. Пожалуйста, попробуйте позже или обратитесь к @gandalfTG.", cancellationToken: cancellationToken);
			return;
		}

        var words = platform.Split(' ');
        var adminWord = words.FirstOrDefault(w => w.StartsWith("@"));

        if (adminWord != null)
        {
            words = words.Where(w => w != adminWord).ToArray();
            admin = adminWord;
            platform = string.Join(" ", words);
        }

        var zakup = new TelegramZakup
        {
            CreatedUtc = DateTime.UtcNow,
            AdPostId = adPostId,
            Platform = platform,
            Price = price,
            Accepted = true,
            ChannelId = adPost.ChannelId,
            InviteLink = link?.InviteLink,
            ZakupSource = ZakupSource.Inline,
            Admin = admin ?? ""
        };

        await _dataContext.AddAsync(zakup, cancellationToken);
        await _dataContext.SaveChangesAsync(cancellationToken);

        try
        {
            //TODO
            // await _sheetsService.AppendRowByHeaders(data.From.Id, zakup.ChannelId, new Dictionary<string, object>()
            // {
            //     ["Дата создания закупа"] = zakup.CreatedUtc,
            //     ["Платформа"] = zakup.Platform ?? "",
            //     ["Цена"] = zakup.Price,
            //     ["Админ"] = zakup.Admin ?? "",
            //     ["Креатив"] = adPost.Title ?? "",
            //     ["Оплачено"] = zakup.IsPad ? "Да" : "Нет",
            //     ["Пригласительная ссылка (не удалять)"] = zakup.InviteLink ?? "",
            //     ["Сейчас в канале"] = 0,
            //     ["Покинуло канал"] = 0,
            //     ["Цена за подписчика(оставшегося)"] = 0,
            //     ["Отписываемость первые 48ч(% от отписавшихся)"] = 0,
            //     ["Премиум пользователей"] = 0,
            //     ["Подписчиков 7+ дней(% от всего вступивших)"] = 0,
            //     ["Клиентов по ссылке"] = 0,
            //     ["Комментирует из подписавшихся(%)"] = 0,
            // });
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.BadRequest && ex.Message.Contains("Unable to parse range"))
        {
            _logger.LogError(ex, "Failed to append row to Google Sheet due to invalid range for user {UserId}", data.From.Id);
            var errorMessage = "С листом вашего проекта возникла проблема. Пожалуйста, восстановите его до состояния до внесения изменений вручную. Если что-то не получается, обратитесь к @gandalfTG";
            await botClient.SendTextMessageAsync(data.From.Id, errorMessage, cancellationToken: cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when appending row to Google Sheet for user {UserId}", data.From.Id);
            var errorMessage = "Произошла ошибка при добавлении строчки в лист проекта на Гугл Таблице. Пожалуйста, попробуйте позже. Если ошибка повторяется, то пожалуйста обратитесь к @gandalfTG";
            await botClient.SendTextMessageAsync(data.From.Id, errorMessage, cancellationToken: cancellationToken);
            return;
        }

        var urlType = link?.CreatesJoinRequest ?? false ? "По заявкам" : "Открытая";
        
        
        //TODO:для чего это
        // if (adPost.File != null && !string.IsNullOrEmpty(adPost.File.ThumbnailId))
        // {
        //     await botClient.EditMessageMediaAsync(data.InlineMessageId,
        //         new InputMediaVideo(InputFile.FromFileId(adPost.File.FileId!)));
        // }

        List<InlineKeyboardButton> keyboard = null;

        if (link is not null)
        {
            keyboard = adPost.Buttons.Select(b => InlineKeyboardButton.WithUrl(b.Text, link.InviteLink)).ToList();

            // Begin of updated code for recalculating entities
            string originalText = adPost.Text;
            List<MessageEntity> originalEntities = adPost.Entities.ToList();

            var regex = new Regex(@"\b(t\.me|https?://t\.me|telegram\.me|https?://telegram\.me)\S*", RegexOptions.Compiled);

            // List to keep track of replacements
            var replacements = new List<(int OldIndex, int OldLength, int NewLength)>();

            // Perform replacements and build new text
            var newTextBuilder = new StringBuilder();
            int currentIndex = 0;

            foreach (Match match in regex.Matches(originalText))
            {
                // Append text before the match
                newTextBuilder.Append(originalText.Substring(currentIndex, match.Index - currentIndex));

                // Record the replacement details
                replacements.Add((OldIndex: match.Index, OldLength: match.Length, NewLength: link.InviteLink.Length));

                // Append the new link
                newTextBuilder.Append(link.InviteLink);

                currentIndex = match.Index + match.Length;
            }

            // Append the remaining text after the last match
            newTextBuilder.Append(originalText.Substring(currentIndex));

            // Update the adPost text
            adPost.Text = newTextBuilder.ToString();

            // Adjust entities after all replacements
            var adjustedEntities = new List<MessageEntity>();

            foreach (var entity in originalEntities)
            {
                int entityStart = entity.Offset;
                int entityEnd = entity.Offset + entity.Length;
                int shift = 0;

                foreach (var replacement in replacements)
                {
                    if (entityEnd <= replacement.OldIndex)
                    {
                        // The entity is before the replacement
                        break;
                    }
                    else if (entityStart >= replacement.OldIndex + replacement.OldLength)
                    {
                        // The entity is after the replacement
                        shift += replacement.NewLength - replacement.OldLength;
                    }
                    else
                    {
                        // The entity overlaps the replacement
                        int overlapStart = Math.Max(entityStart, replacement.OldIndex);
                        int overlapEnd = Math.Min(entityEnd, replacement.OldIndex + replacement.OldLength);

                        // Adjust entity length
                        entity.Length += replacement.NewLength - replacement.OldLength;

                        // If the entity starts within the replacement, move its start
                        if (entityStart >= replacement.OldIndex && entityStart < replacement.OldIndex + replacement.OldLength)
                        {
                            entity.Offset = replacement.OldIndex + shift;
                        }

                        // Only apply the shift for the portion after the replacement
                        if (entityEnd > replacement.OldIndex + replacement.OldLength)
                        {
                            shift += replacement.NewLength - replacement.OldLength;
                        }
                    }
                }

                // Apply the total shift to the entity offset
                entity.Offset += shift;
                if (entity.Offset + entity.Length <= adPost.Text.Length)
                {
                    adjustedEntities.Add(entity);
                }
            }

            adPost.Entities = adjustedEntities;
        }

        // Проверяем длину текста после всех замен
        if (string.IsNullOrWhiteSpace(adPost.Text) || adPost.Text.Length < 2)
        {
            _logger.LogWarning("Текст поста слишком короткий или пустой после замены ссылок. InlineMessageId: {InlineMessageId}", data.InlineMessageId);
            await botClient.SendTextMessageAsync(data.From.Id, "Ошибка: текст поста слишком короткий или пустой после замены ссылок. Пожалуйста, обратитесь к @gandalfTG", cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendTextMessageAsync(353167378, $"Создан закуп.\n Id:[{zakup.Id}] \n Площадка:[{zakup.Platform}] \n Цена:{zakup.Price} ");

        try
        {
            if (string.IsNullOrWhiteSpace(adPost.Text))
            {
                _logger.LogWarning("Попытка отредактировать сообщение с пустым текстом. InlineMessageId: {InlineMessageId}", data.InlineMessageId);
                return;
            }

            await botClient.EditMessageTextAsync(data.InlineMessageId, adPost.Text, entities: adPost.Entities.ToArray(),
                replyMarkup: keyboard is null ? null : new InlineKeyboardMarkup(keyboard),
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"{e.Message} {data.InlineMessageId}", data.From.Id);
            var errorMessage = "ВНИМАНИЕ! Произошла ошибка при замене ссылки в посте через inline. Если ошибка повторяется, то пожалуйста обратитесь к @gandalfTG";
            await botClient.SendTextMessageAsync(data.From.Id, errorMessage, cancellationToken: cancellationToken);
            return;
        }

        var messageBuilder = new StringBuilder($"🔥Запланировано размещение для вашего канала [{adPost.Channel.Title}].");
        messageBuilder.AppendLine("");
        messageBuilder.AppendLine($"Тип ссылки: {urlType}");
        messageBuilder.AppendLine($"💸Цена: {zakup.Price}");
        messageBuilder.AppendLine($"📣Платформа: {zakup.Platform}");
        var postTime = zakup.PostTime is null ? "Не установлено" : zakup.PostTime?.AddHours(3).ToString();
        messageBuilder.AppendLine($"📅Дата публикации: {postTime}");
        messageBuilder.AppendLine($"Креатив: {adPost.Title}");
        messageBuilder.AppendLine("Оплачено: Нет❌");

        // var markUp = new List<InlineKeyboardButton>()
        // {
        //     InlineKeyboardButton.WithCallbackData("⚙️Изменить", $"zakup:post:{ZakupPostFlowType.UPDATE}:{zakup.Id}"),
        //     InlineKeyboardButton.WithCallbackData("🗑Удалить", $"zakup:post:{ZakupPostFlowType.DELETE}:{zakup.Id}"),
        //     InlineKeyboardButton.WithCallbackData("✅Оплачено", $"zakup:post:{ZakupPostFlowType.PAY}:{zakup.Id}")
        // };

        var resultMessage = messageBuilder.ToString();
        {
            try
            {
                await botClient.SendTextMessageAsync(data.From.Id, resultMessage
                    // replyMarkup: new InlineKeyboardMarkup(markUp)
                    );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка отправки после инлайна failed send info to '{username}'", data.From.Id);
            }
        }

        // Проверяем, указан ли 'admin'
        if (!string.IsNullOrEmpty(zakup.Admin))
        {
                // Настраиваем HttpClientHandler для автоматической распаковки сжатых ответов
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            using var client = new HttpClient(handler);

            // Создаем HttpRequestMessage
            var request = new HttpRequestMessage(HttpMethod.Post, "https://telemetr.me/index.php");

            // Устанавливаем заголовки
            request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br"); // Добавляем заголовок Accept-Encoding
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Headers.Add("Origin", "https://telemetr.me");
            request.Headers.Add("Referer", "https://telemetr.me/");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            // Устанавливаем содержимое запроса

            // Устанавливаем содержимое запроса
            var adminParameter = zakup.Admin.TrimStart('@');
            var postData = $"ajax=channel_typeahead&ch=%40{adminParameter}";

            request.Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
            {
                CharSet = "UTF-8"
            };
            request.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(postData);

            // Отправляем запрос
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке POST запроса к telemetr.me для admin: {Admin}", zakup.Admin);
                // Обработка ошибки
                return;
            }

            // Читаем ответ
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Логируем содержимое ответа
            _logger.LogInformation("Ответ от telemetr.me для admin {Admin}: {ResponseContent}", zakup.Admin, responseContent);
    

            // Парсим JSON ответ
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            List<ResponseItem> items;
            try
            {
                items = JsonSerializer.Deserialize<List<ResponseItem>>(responseContent, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при разборе JSON ответа от telemetr.me для admin: {Admin}", zakup.Admin);
                // Обработка ошибки
                return;
            }

            // Проверяем наличие элементов с badlist != ""
            var badItems = items.Where(item => !string.IsNullOrEmpty(item.Badlist)).ToList();
            if (badItems.Any())
            {
                // Отправляем сообщение пользователю
                var message = "⚠️У данного админа обнаружены накрученные каналы:\n" + string.Join("\n", badItems.Select(item => $"Название: {item.Title} Ссылка {item.Link}"));
                await botClient.SendTextMessageAsync(data.From.Id, message, cancellationToken: cancellationToken);

                // Сохраняем admin в базу данных
                // var badAdmin = new BadAdmin
                // {
                //     AdminUsername = admin,
                //     DetectedAt = DateTime.UtcNow,
                //     Reason = "Badlist detected"
                // };

                // await _dataContext.BadAdmins.AddAsync(badAdmin, cancellationToken);
                // await _dataContext.SaveChangesAsync(cancellationToken);
            }
            // Конец добавленного кода
        }

    }
}
