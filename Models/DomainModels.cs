using System.Text.Json.Serialization;

namespace AFK_Engine.Models;

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string AdmiredPerson { get; set; } = string.Empty;
    public string DesiredQuality { get; set; } = string.Empty;
    public string ActivitiesEnjoyed { get; set; } = string.Empty;
    public string DislikesAndAvoidances { get; set; } = string.Empty;
    public string BadScreenHabit { get; set; } = string.Empty;
    public int AvailableTimeMinutes { get; set; } = 30;
    public bool IsInitialized { get; set; } = false;
}

public class Mission
{
    public string Id { get; set; } = $"MIS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int Difficulty { get; set; } // 1 to 5
    public int XpReward { get; set; }   // Difficulty * 10
    public string WhyThisMission { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class MissionResult
{
    public string MissionId { get; set; } = string.Empty;
    public string UserReport { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
