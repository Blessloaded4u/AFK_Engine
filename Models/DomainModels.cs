using System.Text.Json.Serialization;

namespace AFK_Engine.Models;

public class UserProfile
{
    public string Name { get; set; } = string.Empty;
    public string AdmiredPerson { get; set; } = string.Empty;
    public string DesiredQuality { get; set; } = string.Empty;
    public string ActivitiesEnjoyed { get; set; } = string.Empty;
    public string DislikesAndAvoidances { get; set; } = string.Empty;
    public string BadScreenHabit { get; set; } = string.Empty;
    public int AvailableTimeMinutes { get; set; } = 25;
    public bool IsInitialized { get; set; } = false;

    public void SanitizeAndValidate()
    {
        Name = Clean(Name, 40, "Hunter");
        AdmiredPerson = Clean(AdmiredPerson, 60, "A Paragon");
        DesiredQuality = Clean(DesiredQuality, 80, "Discipline and Focus");
        ActivitiesEnjoyed = Clean(ActivitiesEnjoyed, 120, "Physical movement");
        DislikesAndAvoidances = Clean(DislikesAndAvoidances, 120, "Crowded places");
        BadScreenHabit = Clean(BadScreenHabit, 120, "Mindless scrolling");
        AvailableTimeMinutes = Math.Clamp(AvailableTimeMinutes, 10, 180);
    }

    private static string Clean(string input, int maxLen, string fallback)
    {
        var trimmed = input?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return fallback;
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }
}

public class Mission
{
    public string Id { get; set; } = $"AFK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int Difficulty { get; set; } // 1 to 5
    public int XpReward { get; set; }   // Difficulty * 10
    public string WhyThisMission { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED, ABANDONED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MissionReportDto
{
    public string MissionId { get; set; } = string.Empty;
    public string UserReport { get; set; } = string.Empty;
}

public class MissionRecord
{
    public string MissionId { get; set; } = string.Empty;
    public string MissionName { get; set; } = string.Empty;
    public string UserReport { get; set; } = string.Empty;
    public int XpAwarded { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

public class PlayerProgress
{
    public int TotalXp { get; set; } = 0;
    public int Level => (TotalXp / 100) + 1;
    public int CurrentLevelXp => TotalXp % 100;
    public int StreakDays { get; set; } = 0;
    public string? LastCompletedDateString { get; set; } = null; // YYYY-MM-DD
    public int CompletedMissionsCount { get; set; } = 0;
    public int AspirationActionsCompleted { get; set; } = 0;
    public string TomorrowTeaser { get; set; } = "Tomorrow, a new physical trial will test your resolve.";
}

public class HunterState
{
    public string HunterId { get; set; } = string.Empty;
    public UserProfile Profile { get; set; } = new();
    public Mission? CurrentMission { get; set; }
    public PlayerProgress Progress { get; set; } = new();
    public List<MissionRecord> History { get; set; } = new();

    // Synchronization root for atomic state modifications
    [JsonIgnore]
    public readonly object SyncRoot = new();
}

public class AiMissionDto
{
    [JsonPropertyName("missionName")]
    public string MissionName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }

    [JsonPropertyName("whyThisMission")]
    public string WhyThisMission { get; set; } = string.Empty;

    [JsonPropertyName("tomorrowTeaser")]
    public string TomorrowTeaser { get; set; } = string.Empty;
}