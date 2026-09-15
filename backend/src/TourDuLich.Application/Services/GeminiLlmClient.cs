using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace TourDuLich.Application.Services;

public sealed class GeminiLlmClient
{
    private readonly HttpClient _http;
    private readonly string _key;
    private readonly string _model;

    public GeminiLlmClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _key = configuration["Llm:ApiKey"] ?? "";
        _model = configuration["Llm:Model"] ?? "gemini-2.0-flash";
        _http.BaseAddress ??= new Uri("https://generativelanguage.googleapis.com/");
        _http.Timeout = TimeSpan.FromSeconds(45);
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_key);

    public async Task<string?> CompleteWithToolsAsync(
        string system,
        string user,
        object[] functionDeclarations,
        Func<string, JsonElement, CancellationToken, Task<JsonElement>> execute,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled) return null;
        var contents = new List<object>
        {
            new { role = "user", parts = new object[] { new { text = user } } }
        };
        for (var round = 0; round < 6; round++)
        {
            var payload = new
            {
                system_instruction = new { parts = new object[] { new { text = system } } },
                contents,
                tools = new object[] { new { function_declarations = functionDeclarations } },
                tool_config = new { function_calling_config = new { mode = round < 4 ? "AUTO" : "NONE" } }
            };
            using var response = await _http.PostAsJsonAsync(
                $"v1beta/models/{_model}:generateContent?key={Uri.EscapeDataString(_key)}",
                payload, cancellationToken);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            if (!json.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                return null;
            var content = candidates[0].GetProperty("content");
            var parts = content.TryGetProperty("parts", out var p) ? p : default;
            var calls = new List<(string Name, JsonElement Args, string Id)>();
            var texts = new List<string>();
            if (parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("functionCall", out var call) || part.TryGetProperty("function_call", out call))
                    {
                        var name = call.GetProperty("name").GetString() ?? "";
                        var args = call.TryGetProperty("args", out var a) ? a : JsonDocument.Parse("{}").RootElement.Clone();
                        calls.Add((name, args, Guid.NewGuid().ToString("N")));
                    }
                    if (part.TryGetProperty("text", out var text))
                        texts.Add(text.GetString() ?? "");
                }
            }
            if (calls.Count == 0)
                return texts.Count == 0 ? null : string.Join("\n", texts);

            var responseParts = new List<object>();
            foreach (var (name, args, _) in calls)
            {
                var result = await execute(name, args, cancellationToken);
                responseParts.Add(new
                {
                    functionResponse = new { name, response = result }
                });
            }
            contents.Add(new { role = "model", parts = calls.Select(c => (object)new { functionCall = new { name = c.Name, args = c.Args } }).ToArray() });
            contents.Add(new { role = "user", parts = responseParts });
        }
        return null;
    }
}
