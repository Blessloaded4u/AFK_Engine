using System.Collections.Concurrent;
using System.Text.Json;
using AFK_Engine.Models;

namespace AFK_Engine.Services;

public class DataStore
{
    private readonly string _storagePath;
    private static readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly ConcurrentDictionary<string, HunterState> _hunters = new();

    public DataStore(IWebHostEnvironment env)
    {
        _storagePath = Path.Combine(env.ContentRootPath, "hunters_state.json");
        LoadFromFile();
    }

    private void LoadFromFile()
    {
        if (File.Exists(_storagePath))
        {
            try
            {
                var json = File.ReadAllText(_storagePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, HunterState>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        _hunters[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
                // Fallback to empty in-memory store if file reading fails
            }
        }
    }

    /// <summary>
    /// Writes state atomically to a swap file (.tmp) before replacing to prevent corruption.
    /// </summary>
    public async Task SaveToFileAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var tempPath = _storagePath + ".tmp";
            var json = JsonSerializer.Serialize(_hunters, new JsonSerializerOptions { WriteIndented = true });
            
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _storagePath, overwrite: true);
        }
        catch
        {
            // Logging can be attached here in production
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public HunterState GetOrCreateHunter(string hunterId)
    {
        if (string.IsNullOrWhiteSpace(hunterId))
        {
            hunterId = "HNT-" + Guid.NewGuid().ToString()[..8].ToUpper();
        }

        return _hunters.GetOrAdd(hunterId, id => new HunterState { HunterId = id });
    }
}