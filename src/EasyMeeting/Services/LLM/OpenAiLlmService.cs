using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace EasyMeeting.Services.LLM;

public class OpenAiLlmService : ILLmService
{
    private readonly HttpClient _httpClient;
    private readonly Models.LlmConfig _config;
    
    public OpenAiLlmService(Models.LlmConfig config)
    {
        _config = config;
        _httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(config.ApiKey))
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
    }
    
    public async IAsyncEnumerable<string> SummarizeStreamingAsync(
        string text, 
        string language,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var prompt = _config.SummaryPrompt
            .Replace("{language}", language)
            .Replace("{text}", text);
        
        var request = new
        {
            model = _config.Model,
            messages = new[] { new { role = "user", content = prompt } },
            stream = true
        };
        
        var response = await _httpClient.PostAsJsonAsync($"{_config.BaseUrl}/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();
        
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") == true && line != "data: [DONE]")
            {
                var json = line.Substring(6);
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta")
                    .GetProperty("content")
                    .GetString();
                
                if (!string.IsNullOrEmpty(content))
                    yield return content;
            }
        }
    }
    
    public async Task<string> SummarizeAsync(string text, string language, CancellationToken ct = default)
    {
        var result = new System.Text.StringBuilder();
        await foreach (var chunk in SummarizeStreamingAsync(text, language, ct))
        {
            result.Append(chunk);
        }
        return result.ToString();
    }
}
