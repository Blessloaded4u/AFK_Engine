using AFK_Engine.Models;

namespace AFK_Engine.Services;

public static class MissionValidator
{
    // Safety filter to guard against dangerous suggestions
    private static readonly string[] DangerousKeywords = new[]
    {
        "trespass", "climb roof", "highway", "traffic", "illegal", "burn", 
        "weapon", "swim alone", "subway track", "stranger vehicle"
    };

    public static Mission ValidateAndEnforceRules(AiMissionDto? raw, UserProfile profile)
    {
        if (raw == null || 
            string.IsNullOrWhiteSpace(raw.MissionName) || 
            string.IsNullOrWhiteSpace(raw.Description) ||
            ContainsHazard(raw.Description))
        {
            return CreateValidatedFallback(profile);
        }

        var difficulty = Math.Clamp(raw.Difficulty, 1, 5);
        var duration = raw.DurationMinutes > 0 
            ? Math.Min(raw.DurationMinutes, profile.AvailableTimeMinutes)
            : Math.Min(profile.AvailableTimeMinutes, 20);

        return new Mission
        {
            Name = CleanText(raw.MissionName, 60),
            Description = CleanText(raw.Description, 350),
            DurationMinutes = duration,
            Difficulty = difficulty,
            XpReward = difficulty * 10,
            WhyThisMission = string.IsNullOrWhiteSpace(raw.WhyThisMission)
                ? $"Crafted to build {profile.DesiredQuality} through real-world action."
                : CleanText(raw.WhyThisMission, 250),
            Status = "ACTIVE"
        };
    }

    public static Mission CreateValidatedFallback(UserProfile profile)
    {
        var hobby = profile.ActivitiesEnjoyed.ToLowerInvariant();
        string name = "Offline Grounding Protocol";
        string desc = $"Leave all digital screens behind. Step outside and observe your physical neighborhood for {profile.AvailableTimeMinutes} minutes.";

        if (hobby.Contains("music") || hobby.Contains("danc"))
        {
            name = "Rhythm Reset Trial";
            desc = $"Put on music and practice freestyle movement or dance for {profile.AvailableTimeMinutes} minutes without checking notifications.";
        }
        else if (hobby.Contains("read") || hobby.Contains("book"))
        {
            name = "Physical Archive Immersion";
            desc = $"Open a physical book or notebook. Read or write for {profile.AvailableTimeMinutes} minutes in a room away from your phone.";
        }
        else if (hobby.Contains("draw") || hobby.Contains("sketch") || hobby.Contains("art"))
        {
            name = "Physical Contour Recon";
            desc = $"Grab paper and a pen. Step outside or to a window and sketch three physical structures for {profile.AvailableTimeMinutes} minutes.";
        }

        return new Mission
        {
            Name = name,
            Description = desc,
            DurationMinutes = profile.AvailableTimeMinutes,
            Difficulty = 3,
            XpReward = 30,
            WhyThisMission = $"Disengaging from {profile.BadScreenHabit} strengthens the focus needed to develop {profile.DesiredQuality}.",
            Status = "ACTIVE"
        };
    }

    private static bool ContainsHazard(string text)
    {
        var lower = text.ToLowerInvariant();
        return DangerousKeywords.Any(k => lower.Contains(k));
    }

    private static string CleanText(string input, int maxLen)
    {
        var t = input.Trim().Replace('\r', ' ').Replace('\n', ' ');
        return t.Length <= maxLen ? t : t[..maxLen] + "...";
    }
}