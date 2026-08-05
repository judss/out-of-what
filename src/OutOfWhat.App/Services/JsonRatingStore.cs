using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OutOfWhatApp.Models;

namespace OutOfWhatApp.Services;

public class JsonRatingStore : IRatingStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonRatingStore()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OutOfWhat");
        Directory.CreateDirectory(appDataDir);
        _filePath = Path.Combine(appDataDir, "history.json");
    }

    public async Task AddAsync(RatingEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = await ReadAllAsync();
            var updated = new List<RatingEntry>(entries) { entry };
            await WriteAllAsync(updated);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<RatingEntry>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await ReadAllAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<RatingEntry>> ReadAllAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<RatingEntry>();
        }

        await using var stream = File.OpenRead(_filePath);
        var entries = await JsonSerializer.DeserializeAsync<List<RatingEntry>>(stream);
        return entries ?? new List<RatingEntry>();
    }

    private async Task WriteAllAsync(List<RatingEntry> entries)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, entries, new JsonSerializerOptions { WriteIndented = true });
    }
}
