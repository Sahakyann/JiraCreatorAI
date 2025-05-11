using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GeminiService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient = new();
    private const string GeminiProModel = "gemini-1.5-pro-latest";
    private const string ApiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/";

    public GeminiService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> GenerateTicketAsync(string input)
    {
        var prompt = new
        {
            contents = new[] {
                new {
                    parts = new[] {
                        new { text = $"You are a ticket creation assistant. Generate a formatted ticket.\n\nInput:\n{input}" }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(prompt), Encoding.UTF8, "application/json");
        var requestUri = $"{ApiEndpoint}{GeminiProModel}:generateContent";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("x-goog-api-key", _config["GeminiApiKey"]);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);

        var json = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Error generating ticket.";
            }
            catch (JsonException ex)
            {
       
                return $"Error parsing JSON response: {ex.Message}";
            }
        }
        else
        {
 
            return $"Error generating ticket. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {json}";
        }
    }

    public async Task<string> ListAvailableModelsAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models");
        request.Headers.Add("x-goog-api-key", _config["GeminiApiKey"]);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        return $"List Models Response (Status: {response.StatusCode}): {json}";
    }
}