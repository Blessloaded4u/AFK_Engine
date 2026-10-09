using System.Text.Json;
using AFK_Engine.Models;

namespace AFK_Engine.Services;

public class DataStore
{
    private readonly string _filePath;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private SystemState _state = new();

    public DataStore(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "system_state.json");
        LoadState();
    }

    private void LoadState()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                _state = JsonSerializer.Deserialize<SystemState>(json) ?? new SystemState();
            }
            catch
            {
                _state = new SystemState();
            }
        }
    }

    public async Task SaveStateAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    public SystemState GetState() => _state;
}