using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheSystem.Models;

namespace TheSystem.Services;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        
        // Read key from Environment Variable or appsettings.json
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") 
                  ?? config["GroqApiKey"] 
                  ?? string.Empty;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public async Task<AiMissionDto> GenerateMissionAsync(UserProfile profile, PlayerProgress progress)
    {
        // Fail-safe: procedural fallback if no API key is provided yet
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new AiMissionDto
            {
                MissionName = "Sever The Digital Wire",
                Description = $"Leave your phone indoors. Step outside and walk for {profile.AvailableTimeMinutes} minutes without music or screens.",
                DurationMinutes = profile.AvailableTimeMinutes,
                Difficulty = 3,
                WhyThisMission = $"You aspire to build {profile.DesiredQuality} like {profile.AdmiredPerson}. Stepping away from {profile.BadScreenHabit} proves you are in control.",
                TomorrowTeaser = "Tomorrow's intervention will test your eye for detail in the physical world."
            };
        }

        var systemPrompt = @"You are THE SYSTEM: an objective, Solo Leveling-inspired AI progression engine.
Your sole purpose is to create real-world interventions that force the user away from their screen.
You MUST reply with RAW JSON ONLY. No markdown (```json), no conversational filler.";

        var userPrompt = $@"
Hunter Profile:
- Name: {profile.Name}
- Target Trait: {profile.DesiredQuality} (Inspired by: {profile.AdmiredPerson})
- Likes: {profile.ActivitiesEnjoyed}
- Dislikes/Avoids: {profile.DislikesAndAvoidances}
- Digital Habit to Break: {profile.BadScreenHabit}
- Time Budget: {profile.AvailableTimeMinutes} minutes
- Current Level: {progress.Level}

Output strict JSON with this exact schema:
{{
  ""missionName"": ""Short quest name"",
  ""description"": ""Direct, clear physical action to perform outside/away from screens"",
  ""durationMinutes"": {profile.AvailableTimeMinutes},
  ""difficulty"": integer between 1 and 5,
  ""whyThisMission"": ""Psychological explanation linking this task to their aspiration"",
  ""tomorrowTeaser"": ""A short, mysterious hint about tomorrow's trial""
}}";

        var payload = new
        {
            model = "gemma2-9b-it", // Google's open-weight Gemma model on Groq
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.7
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("chat/completions", payload);
            response.EnsureSuccessStatusCode();

            var groqRes = await response.Content.ReadFromJsonAsync<GroqApiResponse>();
            var rawJson = groqRes?.Choices?.FirstOrDefault()?.Message?.Content ?? "{}";

            var dto = JsonSerializer.Deserialize<AiMissionDto>(rawJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return dto ?? throw new Exception("Failed to deserialize mission DTO.");
        }
        catch
        {
            // Fallback so user is never blocked if network/API fails
            return new AiMissionDto
            {
                MissionName = "Perimeter Recon",
                Description = $"Walk outside for {Math.Min(profile.AvailableTimeMinutes, 20)} minutes. Find three physical objects you have never noticed before.",
                DurationMinutes = Math.Min(profile.AvailableTimeMinutes, 20),
                Difficulty = 2,
                WhyThisMission = "A mind consumed by digital feeds misses the physical world. Reset your observation baseline.",
                TomorrowTeaser = "Tomorrow, physical endurance will be demanded."
            };
        }
    }
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