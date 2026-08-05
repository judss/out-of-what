using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OutOfWhatApp.Services;

public class JsonDailyRollProvider : IDailyRollProvider
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Random _random = new();

    public JsonDailyRollProvider()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OutOfWhat");
        Directory.CreateDirectory(appDataDir);
        _filePath = Path.Combine(appDataDir, "dailyroll.json");
    }

    public async Task<int> GetOrCreateTodayDenominatorAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            var existing = await ReadAsync();
            if (existing is not null && existing.Date == today)
            {
                return existing.Denominator;
            }

            var denominator = RollDenominator();
            await WriteAsync(new DailyRoll(today, denominator));
            return denominator;
        }
        finally
        {
            _lock.Release();
        }
    }

    private int RollDenominator()
    {
        // Mild skew away from the low end of the range (10 is rare, higher values more common).
        var r = _random.NextDouble();
        return 10 + (int)(989 * Math.Pow(r, 0.6));
    }

    private async Task<DailyRoll?> ReadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<DailyRoll>(stream);
    }

    private async Task WriteAsync(DailyRoll roll)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, roll, new JsonSerializerOptions { WriteIndented = true });
    }

    private record DailyRoll(
        [property: JsonPropertyName("date")] DateOnly Date,
        [property: JsonPropertyName("denominator")] int Denominator);
}
