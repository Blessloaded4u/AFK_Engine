using AFK_Engine.Models;
using AFK_Engine.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<AiService>();
builder.Services.AddSingleton<DataStore>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Extract Hunter ID and Client Local Date from headers
string GetHunterId(HttpContext context) =>
    context.Request.Headers.TryGetValue("X-Hunter-Id", out var id) && !string.IsNullOrWhiteSpace(id)
        ? id.ToString()
        : "HNT-" + Guid.NewGuid().ToString()[..8].ToUpper();

string GetClientDate(HttpContext context) =>
    context.Request.Headers.TryGetValue("X-Client-Date", out var d) && !string.IsNullOrWhiteSpace(d)
        ? d.ToString()
        : DateTime.UtcNow.ToString("yyyy-MM-dd");

// 1. Get State
app.MapGet("/api/state", (HttpContext ctx, DataStore store) =>
{
    var hunterId = GetHunterId(ctx);
    var state = store.GetOrCreateHunter(hunterId);
    return Results.Ok(state);
});

// 2. Initialize / Update Profile with Server-Side Validation
app.MapPost("/api/profile", async (HttpContext ctx, UserProfile profile, DataStore store, AiService ai) =>
{
    var hunterId = GetHunterId(ctx);
    var state = store.GetOrCreateHunter(hunterId);

    // Enforce server-side sanitization and limits (10-180 mins)
    profile.SanitizeAndValidate();

    lock (state.SyncRoot)
    {
        state.Profile = profile;
        state.Profile.IsInitialized = true;
    }

    // Generate first mission
    var aiDto = await ai.GenerateMissionAsync(state.Profile, state.Progress, state.History);
    var mission = MissionValidator.ValidateAndEnforceRules(aiDto, state.Profile);

    lock (state.SyncRoot)
    {
        state.CurrentMission = mission;
        state.Progress.TomorrowTeaser = aiDto?.TomorrowTeaser ?? "Rest well. The System prepares tomorrow's trial.";
    }

    await store.SaveToFileAsync();
    return Results.Ok(state);
});

// 3. Thread-Safe Atomic Mission Completion (Prevents Duplicate XP)
app.MapPost("/api/mission/complete", async (HttpContext ctx, MissionReportDto report, DataStore store) =>
{
    var hunterId = GetHunterId(ctx);
    var state = store.GetOrCreateHunter(hunterId);
    var clientDate = GetClientDate(ctx); // Client local date YYYY-MM-DD

    lock (state.SyncRoot)
    {
        // 1. Verify that an active mission exists
        if (state.CurrentMission == null || state.CurrentMission.Status != "ACTIVE")
        {
            return Results.Conflict(new { error = "No active quest is available to complete." });
        }

        // 2. Verify submission integrity
        if (state.CurrentMission.Id != report.MissionId)
        {
            return Results.BadRequest(new { error = "Submitted quest ID does not match the active mission." });
        }

        // 3. Atomically transition state to prevent concurrent double-rewards
        state.CurrentMission.Status = "COMPLETED";

        var cleanReport = string.IsNullOrWhiteSpace(report.UserReport) 
            ? "Completed directive in the physical world." 
            : report.UserReport.Trim();

        // 4. Award deterministic XP
        state.Progress.TotalXp += state.CurrentMission.XpReward;
        state.Progress.CompletedMissionsCount += 1;
        state.Progress.AspirationActionsCompleted += 1;

        // 5. Calculate streak by calendar day (using client's local date)
        if (state.Progress.LastCompletedDateString == null)
        {
            state.Progress.StreakDays = 1;
            state.Progress.LastCompletedDateString = clientDate;
        }
        else
        {
            if (DateTime.TryParse(state.Progress.LastCompletedDateString, out var lastDate) &&
                DateTime.TryParse(clientDate, out var currentDate))
            {
                var diffDays = (currentDate.Date - lastDate.Date).Days;
                if (diffDays == 1)
                {
                    state.Progress.StreakDays += 1;
                    state.Progress.LastCompletedDateString = clientDate;
                }
                else if (diffDays > 1)
                {
                    state.Progress.StreakDays = 1; // Broken streak reset
                    state.Progress.LastCompletedDateString = clientDate;
                }
                // If diffDays == 0 (completed another quest on same day), streak remains unchanged
            }
        }

        // 6. Record history
        state.History.Add(new MissionRecord
        {
            MissionId = state.CurrentMission.Id,
            MissionName = state.CurrentMission.Name,
            UserReport = cleanReport,
            XpAwarded = state.CurrentMission.XpReward
        });
    }

    await store.SaveToFileAsync();
    return Results.Ok(state);
});

// 4. Request Next Mission (Lifecycle Guard: Cannot replace active mission)
app.MapPost("/api/mission/next", async (HttpContext ctx, DataStore store, AiService ai) =>
{
    var hunterId = GetHunterId(ctx);
    var state = store.GetOrCreateHunter(hunterId);

    if (!state.Profile.IsInitialized)
    {
        return Results.BadRequest(new { error = "Profile is not initialized." });
    }

    lock (state.SyncRoot)
    {
        if (state.CurrentMission != null && state.CurrentMission.Status == "ACTIVE")
        {
            return Results.Conflict(new { error = "You have an active directive in progress. Complete or abandon it before requesting another." });
        }
    }

    var aiDto = await ai.GenerateMissionAsync(state.Profile, state.Progress, state.History);
    var mission = MissionValidator.ValidateAndEnforceRules(aiDto, state.Profile);

    lock (state.SyncRoot)
    {
        state.CurrentMission = mission;
        state.Progress.TomorrowTeaser = aiDto?.TomorrowTeaser ?? "Rest tonight. The System will adapt to your growth.";
    }

    await store.SaveToFileAsync();
    return Results.Ok(state);
});

// 5. Explicitly Abandon Mission (Allows moving forward without cheating XP)
app.MapPost("/api/mission/abandon", async (HttpContext ctx, DataStore store) =>
{
    var hunterId = GetHunterId(ctx);
    var state = store.GetOrCreateHunter(hunterId);

    lock (state.SyncRoot)
    {
        if (state.CurrentMission == null || state.CurrentMission.Status != "ACTIVE")
        {
            return Results.BadRequest(new { error = "No active mission to abandon." });
        }

        state.CurrentMission.Status = "ABANDONED";
    }

    await store.SaveToFileAsync();
    return Results.Ok(state);
});

// 6. Reset Profile
app.MapPost("/api/reset", async (HttpContext ctx, DataStore store) =>
{
    var hunterId = GetHunterId(ctx);
    var state = store.GetOrCreateHunter(hunterId);

    lock (state.SyncRoot)
    {
        state.Profile = new UserProfile();
        state.CurrentMission = null;
        state.Progress = new PlayerProgress();
        state.History.Clear();
    }

    await store.SaveToFileAsync();
    return Results.Ok(state);
});

app.Run();