using TheSystem.Models;
using TheSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient<AiService>();
builder.Services.AddSingleton<DataStore>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// --- API ENDPOINTS ---

// 1. Get current full system state
app.MapGet("/api/state", (DataStore store) =>
{
    return Results.Ok(store.GetState());
});

// 2. Initialize profile (Onboarding)
app.MapPost("/api/profile", async (UserProfile profile, DataStore store, AiService ai) =>
{
    var state = store.GetState();
    state.Profile = profile;
    state.Profile.IsInitialized = true;

    // Immediately generate the first mission via AI
    var aiDto = await ai.GenerateMissionAsync(state.Profile, state.Progress);
    
    // Application enforces game rules (Architecture B)
    var difficulty = Math.Clamp(aiDto.Difficulty, 1, 5);
    state.CurrentMission = new Mission
    {
        Name = aiDto.MissionName,
        Description = aiDto.Description,
        DurationMinutes = Math.Min(aiDto.DurationMinutes, state.Profile.AvailableTimeMinutes),
        Difficulty = difficulty,
        XpReward = difficulty * 10, // Deterministic: 10 to 50 XP
        WhyThisMission = aiDto.WhyThisMission,
        Status = "ACTIVE"
    };

    state.Progress.TomorrowTeaser = aiDto.TomorrowTeaser;
    await store.SaveStateAsync();

    return Results.Ok(state);
});

// 3. Complete Mission & Report Action
app.MapPost("/api/mission/complete", async (MissionResult report, DataStore store) =>
{
    var state = store.GetState();
    if (state.CurrentMission == null || state.CurrentMission.Status != "ACTIVE")
    {
        return Results.BadRequest("No active quest to complete.");
    }

    // Mark current mission done
    state.CurrentMission.Status = "COMPLETED";
    state.History.Add(report);

    // Deterministic Progression Calculation
    state.Progress.TotalXp += state.CurrentMission.XpReward;
    state.Progress.CompletedMissionsCount += 1;
    state.Progress.StreakDays += 1;

    await store.SaveStateAsync();
    return Results.Ok(state);
});

// 4. Request Next Mission
app.MapPost("/api/mission/next", async (DataStore store, AiService ai) =>
{
    var state = store.GetState();
    if (!state.Profile.IsInitialized)
    {
        return Results.BadRequest("Profile not initialized.");
    }

    var aiDto = await ai.GenerateMissionAsync(state.Profile, state.Progress);
    var difficulty = Math.Clamp(aiDto.Difficulty, 1, 5);

    state.CurrentMission = new Mission
    {
        Name = aiDto.MissionName,
        Description = aiDto.Description,
        DurationMinutes = Math.Min(aiDto.DurationMinutes, state.Profile.AvailableTimeMinutes),
        Difficulty = difficulty,
        XpReward = difficulty * 10,
        WhyThisMission = aiDto.WhyThisMission,
        Status = "ACTIVE"
    };

    state.Progress.TomorrowTeaser = aiDto.TomorrowTeaser;
    await store.SaveStateAsync();

    return Results.Ok(state);
});

// 5. Reset System (For testing/debugging)
app.MapPost("/api/reset", async (DataStore store) =>
{
    var state = store.GetState();
    state.Profile = new UserProfile();
    state.CurrentMission = null;
    state.Progress = new PlayerProgress();
    state.History.Clear();
    await store.SaveStateAsync();
    return Results.Ok(state);
});

app.Run();