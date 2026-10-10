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
    private string? _selectedModel = null;

    public AiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");

        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") 
                  ?? config["GroqApiKey"] 
                  ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.Contains("YOUR_ACTUAL_KEY"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[AFK ENGINE WARNING] GroqApiKey is missing or empty.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[AFK ENGINE READY] Groq API Key loaded successfully.");
            Console.ResetColor();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    /// <summary>
    /// Auto-detects the best active model available on your Groq account
    /// </summary>
    private async Task<string> ResolveActiveModelAsync()
    {
        if (!string.IsNullOrEmpty(_selectedModel))
            return _selectedModel;

        try
        {
            Console.WriteLine("[AFK ENGINE] Discovering available models on your Groq account...");
            var res = await _httpClient.GetFromJsonAsync<GroqModelListResponse>("models");
            
            if (res?.Data != null && res.Data.Count > 0)
            {
                // Filter out audio/whisper models
                var textModels = res.Data
                    .Select(m => m.Id)
                    .Where(id => !id.Contains("whisper", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[AFK ENGINE] Active text models on Groq: {string.Join(", ", textModels)}");
                Console.ResetColor();

                // Preferred fast open-weight models in order of priority
                var preferences = new[] { "gpt-oss-20b", "gpt-oss-120b", "qwen", "llama" };
                foreach (var pref in preferences)
                {
                    var match = textModels.FirstOrDefault(m => m.Contains(pref, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        _selectedModel = match;
                        break;
                    }
                }

                // If no preference matched, use the first available text model
                _selectedModel ??= textModels.FirstOrDefault() ?? "openai/gpt-oss-20b";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AFK ENGINE Warning] Could not query /models endpoint: {ex.Message}");
            _selectedModel = "openai/gpt-oss-20b";
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[AFK ENGINE Auto-Selector] Locked active model -> {_selectedModel}");
        Console.ResetColor();
        return _selectedModel!;
    }

    public async Task<AiMissionDto> GenerateMissionAsync(UserProfile profile, PlayerProgress progress, List<MissionResult> history)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.Contains("YOUR_ACTUAL_KEY"))
        {
            return GeneratePersonalizedFallback(profile);
        }

        var activeModel = await ResolveActiveModelAsync();

        var previousReports = history.TakeLast(3).Select(h => h.UserReport).ToList();
        var historyContext = previousReports.Count > 0 
            ? string.Join("; ", previousReports) 
            : "No previous quests recorded.";

        var systemPrompt = @"You are THE SYSTEM: a personalized, Solo Leveling-inspired progression engine.
Your goal is to issue engaging real-world actions that get the Hunter away from screen addiction.

CRITICAL RULES:
1. The quest MUST directly adapt to the user's specific hobbies and likes (e.g. if they love dancing/music, the quest MUST be dance or music practice away from screens; if they love reading, it must involve reading physical pages or visiting a bookstore/library; if art, physical sketching; if workout, bodyweight training).
2. DO NOT default to walking or running unless their primary hobby explicitly asks for walking or hiking.
3. The activity must be done OFFLINE in the real physical world, with no digital screens or mindless phone scrolling.
4. Respond with RAW JSON ONLY. No markdown, no conversational commentary.
5. The goal is to make the user leave the screen and enjoy the outside world.
6. You can also apply psychological theories that can help to even convince the user to leave the screen using what the user likes and hates";

        var userPrompt = $@"
Hunter Profile:
- Name: {profile.Name}
- Target Quality: {profile.DesiredQuality} (Aspirational Model: {profile.AdmiredPerson})
- Core Passions/Hobbies: {profile.ActivitiesEnjoyed}
- Dislikes & Environments to Avoid: {profile.DislikesAndAvoidances}
- Bad Screen Habit to Overcome: {profile.BadScreenHabit}
- Available Time: {profile.AvailableTimeMinutes} minutes
- Level: {progress.Level}

Previous Missions (Do NOT repeat): {historyContext}

Generate a creative real-world quest tailored to their core passion ({profile.ActivitiesEnjoyed}).

Output exact JSON schema:
{{
  ""missionName"": ""Creative quest title"",
  ""description"": ""Direct, clear physical action to perform right now away from screens"",
  ""durationMinutes"": {profile.AvailableTimeMinutes},
  ""difficulty"": integer between 1 and 5,
  ""whyThisMission"": ""1-2 sentences explaining why this exact action channels their passion for {profile.ActivitiesEnjoyed} to build {profile.DesiredQuality}"",
  ""tomorrowTeaser"": ""A short, mysterious hint about tomorrow's trial""
}}";

        var payload = new
        {
            model = activeModel,
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
            Console.WriteLine($"[AFK ENGINE] Calling Groq ({activeModel}) for Hunter: {profile.Name}...");
            var response = await _httpClient.PostAsJsonAsync("chat/completions", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Groq API Error {response.StatusCode}]: {errorBody}");
                Console.ResetColor();
                return GeneratePersonalizedFallback(profile);
            }

            var groqRes = await response.Content.ReadFromJsonAsync<GroqApiResponse>();
            var rawJson = groqRes?.Choices?.FirstOrDefault()?.Message?.Content ?? "{}";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[AFK ENGINE] Quest generated successfully:\n{rawJson}");
            Console.ResetColor();

            var dto = JsonSerializer.Deserialize<AiMissionDto>(rawJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return dto ?? GeneratePersonalizedFallback(profile);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[AI Service Error]: {ex.Message}");
            Console.ResetColor();
            return GeneratePersonalizedFallback(profile);
        }
    }

    private static AiMissionDto GeneratePersonalizedFallback(UserProfile profile)
    {
        var hobby = profile.ActivitiesEnjoyed.ToLower();
        string name = "Offline Mastery Trial";
        string desc = $"Spend {profile.AvailableTimeMinutes} minutes practicing {profile.ActivitiesEnjoyed} without touching your phone.";

        if (hobby.Contains("music") || hobby.Contains("danc"))
        {
            name = "Rhythm Immersion Protocol";
            desc = $"Put on your favorite music and dance or practice rhythm for {Math.Min(profile.AvailableTimeMinutes, 20)} minutes non-stop. No checking screens or texts.";
        }
        else if (hobby.Contains("read") || hobby.Contains("book"))
        {
            name = "Silent Archives";
            desc = $"Open a physical book or journal. Read or write for {profile.AvailableTimeMinutes} minutes in silence with your phone in another room.";
        }
        else if (hobby.Contains("draw") || hobby.Contains("art") || hobby.Contains("paint"))
        {
            name = "Real-World Capture";
            desc = $"Take a pen and paper. Sketch three real physical objects in your immediate surroundings for {profile.AvailableTimeMinutes} minutes.";
        }

        return new AiMissionDto
        {
            MissionName = name,
            Description = desc,
            DurationMinutes = profile.AvailableTimeMinutes,
            Difficulty = 3,
            WhyThisMission = $"Channeling your passion for {profile.ActivitiesEnjoyed} offline helps break the hold of {profile.BadScreenHabit}.",
            TomorrowTeaser = "Tomorrow's intervention will demand fresh creative energy."
        };
    }
}

// Helper models for discovering active Groq models
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