using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Zakup.Services;

public class TelemetrService
{
    private readonly ILogger<TelemetrService> _logger;
    private readonly HttpClient _httpClient;

    public TelemetrService(ILogger<TelemetrService> logger)
    {
        _logger = logger;
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli };
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://telemetr.me");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://telemetr.me/");
        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public record ResponseItem(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("participants_count")] string ParticipantsCount,
        [property: JsonPropertyName("ava")] string Ava,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("link")] string Link,
        [property: JsonPropertyName("analytics_link")] string AnalyticsLink,
        [property: JsonPropertyName("channel_id")] string ChannelId,
        [property: JsonPropertyName("badlist")] string Badlist,
        [property: JsonPropertyName("typed")] string Typed
    );

    public async Task<List<ResponseItem>> CheckAdminChannels(string adminUsername, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://telemetr.me/index.php");
            var adminParameter = adminUsername.TrimStart('@');
            var postData = $"ajax=channel_typeahead&ch=%40{adminParameter}";
            request.Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "UTF-8" };
            request.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(postData);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Ответ от telemetr.me для admin {Admin}: {ResponseContent}", adminUsername, responseContent);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<ResponseItem>>(responseContent, options) ?? new List<ResponseItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке каналов админа {Admin}", adminUsername);
            return new List<ResponseItem>();
        }
    }
} 