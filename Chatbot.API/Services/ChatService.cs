using System.Text;
using System.Text.Json;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private static DateTime _lastCallTime = DateTime.MinValue;

    public ChatService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["OpenAI:ApiKey"]; // get from appsettings.json or environment
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastCall = DateTime.UtcNow - _lastCallTime;
            if (timeSinceLastCall.TotalSeconds < 20)
            {
                await Task.Delay(TimeSpan.FromSeconds(20) - timeSinceLastCall);
            }

            _lastCallTime = DateTime.UtcNow;

            var url = "https://api.openai.com/v1/chat/completions";

            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = userMessage }
            }
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(jsonResponse);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                return content ?? "No response content.";
            }

            var errorText = await response.Content.ReadAsStringAsync();
            return $"Error: {response.StatusCode} - {errorText}";
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }
}