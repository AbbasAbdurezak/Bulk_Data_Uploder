public class WebhookClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookClient> _logger;

    public WebhookClient(HttpClient httpClient, ILogger<WebhookClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendWebhookAsync(string url, object payload)
    {
        var retries = 3;
        for (int attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                response.EnsureSuccessStatusCode();
                break;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Webhook request failed. Attempt {attempt}/{retries}", attempt, retries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
    }
}