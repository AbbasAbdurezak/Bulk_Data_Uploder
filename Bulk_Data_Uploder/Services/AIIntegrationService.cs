public class AIIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AIIntegrationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<DataRecord>> ProcessDataAsync(List<DataRecord> records)
    {
        var baseUrl = _configuration["DeepSeek:BaseUrl"];
        var apiKey = _configuration["DeepSeek:ApiKey"];

        var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/process", records);
        response.EnsureSuccessStatusCode();

        var processedRecords = await response.Content.ReadFromJsonAsync<List<DataRecord>>();
        return processedRecords;
    }
}