using System.Text.Json.Serialization;

namespace TheSystem.Models;

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
