using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AFK_Engine.Models;

namespace AFK_Engine.Services;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private string? _activeModel;

    public AiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");

        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") 
                  ?? config["GroqApiKey"] 
                  ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    private async Task<string> ResolveModelAsync()
    {
        if (!string.IsNullOrEmpty(_activeModel)) return _activeModel;

        try
        {
            var res = await _httpClient.GetFromJsonAsync<GroqModelListResponse>("models");
            if (res?.Data != null && res.Data.Count > 0)
            {
                var textModels = res.Data.Select(m => m.Id)
                    .Where(id => !id.Contains("whisper", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var prefs = new[] { "gpt-oss-20b", "gpt-oss-120b", "qwen", "llama" };
                foreach (var pref in prefs)
                {
                    var match = textModels.FirstOrDefault(m => m.Contains(pref, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        _activeModel = match;
                        break;
                    }
                }
                _activeModel ??= textModels.FirstOrDefault() ?? "openai/gpt-oss-20b";
            }
        }
        catch
        {
            _activeModel = "openai/gpt-oss-20b";
        }

        return _activeModel!;
    }

    public async Task<AiMissionDto?> GenerateMissionAsync(UserProfile profile, PlayerProgress progress, List<MissionRecord> history)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return null; // Triggers safety fallback
        }

        var model = await ResolveModelAsync();
        var recentTasks = history.TakeLast(3).Select(h => h.MissionName);
        var historySummary = history.Count > 0 ? string.Join(", ", recentTasks) : "None";

        var systemPrompt = @"You are AFK ENGINE: an offline progression system.
Generate an engaging real-world directive that gets the user away from their screen.

RULES:
1. Ground the quest in the user's hobbies (reading -> physical library/paper books; dancing -> dance freestyle; exploration -> outside perimeter).
2. Prioritize OUTDOOR or screen-free movement.
3. Ensure tasks are safe, legal, and non-hazardous.
4. Output RAW JSON ONLY matching the requested schema. No conversational markdown.";

        var userPrompt = $@"
Hunter Context:
- Name: {profile.Name}
- Target Trait: {profile.DesiredQuality} (Admired: {profile.AdmiredPerson})
- Core Passions: {profile.ActivitiesEnjoyed}
- Dislikes to Avoid: {profile.DislikesAndAvoidances}
- Bad Screen Habit: {profile.BadScreenHabit}
- Available Time: {profile.AvailableTimeMinutes} minutes
- Level: {progress.Level}
- Avoid Repeating: {historySummary}

Output exact JSON schema:
{{
  ""missionName"": ""Creative quest title"",
  ""description"": ""Direct, clear physical action to perform right now away from screens"",
  ""durationMinutes"": {profile.AvailableTimeMinutes},
  ""difficulty"": integer between 1 and 5,
  ""whyThisMission"": ""1-2 sentences on how this develops {profile.DesiredQuality}"",
  ""tomorrowTeaser"": ""A short hint about tomorrow's trial""
}}";

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.85
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("chat/completions", payload);
            if (!response.IsSuccessStatusCode) return null;

            var groqRes = await response.Content.ReadFromJsonAsync<GroqApiResponse>();
            var rawJson = groqRes?.Choices?.FirstOrDefault()?.Message?.Content ?? "{}";

            return JsonSerializer.Deserialize<AiMissionDto>(rawJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
        catch
        {
            return null;
        }
    }
}

// --- DTO Classes for Groq API Deserialization ---

public class GroqModelListResponse
{
    [JsonPropertyName("data")]
    public List<GroqModelItem> Data { get; set; } = new();
}

public class GroqModelItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class GroqApiResponse
{
    [JsonPropertyName("choices")]
    public List<GroqChoice> Choices { get; set; } = new();
}

public class GroqChoice
{
    [JsonPropertyName("message")]
    public GroqMessage Message { get; set; } = new();
}

public class GroqMessage
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}