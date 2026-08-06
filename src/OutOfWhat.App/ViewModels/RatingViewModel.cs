using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutOfWhatApp.Models;
using OutOfWhatApp.Services;

namespace OutOfWhatApp.ViewModels;

public partial class RatingViewModel : ObservableObject
{
    private const int MaxNumerator = 100_000;

    private readonly IRatingStore _ratingStore;
    private readonly IDailyRollProvider _dailyRollProvider;

    [ObservableProperty]
    private string? _numeratorText;

    [ObservableProperty]
    private int _denominator = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationError))]
    private string? _validationError;

    public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOverageWarning))]
    private string? _overageWarning;

    public bool HasOverageWarning => !string.IsNullOrEmpty(OverageWarning);

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public RatingViewModel(IRatingStore ratingStore, IDailyRollProvider dailyRollProvider)
    {
        _ratingStore = ratingStore;
        _dailyRollProvider = dailyRollProvider;
    }

    public async Task LoadTodayAsync()
    {
        Denominator = await _dailyRollProvider.GetOrCreateTodayDenominatorAsync();
        NumeratorText = "";
        ValidationError = null;
        OverageWarning = null;
    }

    partial void OnNumeratorTextChanged(string? value)
    {
        ValidationError = null;

        OverageWarning = int.TryParse(value, out var numerator) && numerator > Denominator
            ? "You scored higher than today's target.\nAre you sure you want to log this?"
            : null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationError = null;

        if (!int.TryParse(NumeratorText, out var numerator))
        {
            ValidationError = "Enter a number to log your day.";
            return;
        }

        if (numerator < 0)
        {
            ValidationError = "Rating can't be negative.";
            return;
        }

        if (numerator > MaxNumerator)
        {
            ValidationError = "That's a bit too high — keep it under 100,000.";
            return;
        }

        await SaveEntryAsync(numerator);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveEntryAsync(int numerator)
    {
        var entry = new RatingEntry(DateTimeOffset.Now, numerator, Denominator, Note: null);
        await _ratingStore.AddAsync(entry);

        Saved?.Invoke(this, EventArgs.Empty);
    }
}
