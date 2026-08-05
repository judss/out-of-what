using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutOfWhatApp.Models;
using OutOfWhatApp.Services;

namespace OutOfWhatApp.ViewModels;

public partial class RatingViewModel : ObservableObject
{
    private readonly IRatingStore _ratingStore;
    private readonly IDailyRollProvider _dailyRollProvider;

    [ObservableProperty]
    private int _numerator;

    [ObservableProperty]
    private int _denominator = 10;

    [ObservableProperty]
    private string? _gratitude;

    public event EventHandler? Saved;

    public RatingViewModel(IRatingStore ratingStore, IDailyRollProvider dailyRollProvider)
    {
        _ratingStore = ratingStore;
        _dailyRollProvider = dailyRollProvider;
    }

    public async Task LoadTodayAsync()
    {
        Denominator = await _dailyRollProvider.GetOrCreateTodayDenominatorAsync();
        Numerator = 0;
        Gratitude = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var entry = new RatingEntry(DateTimeOffset.Now, Numerator, Denominator, Gratitude);
        await _ratingStore.AddAsync(entry);

        Saved?.Invoke(this, EventArgs.Empty);
    }
}
