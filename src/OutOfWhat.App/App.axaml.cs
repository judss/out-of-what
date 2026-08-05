using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using OutOfWhatApp.Platform.MacOS;
using OutOfWhatApp.Services;
using OutOfWhatApp.ViewModels;
using OutOfWhatApp.Views;

namespace OutOfWhatApp;

public partial class App : Application
{
    private IRatingStore _ratingStore = null!;
    private IDailyRollProvider _dailyRollProvider = null!;
    private RatingViewModel _ratingViewModel = null!;
    private RatingPopup _ratingPopup = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _ratingStore = new JsonRatingStore();
            _dailyRollProvider = new JsonDailyRollProvider();
            _ratingViewModel = new RatingViewModel(_ratingStore, _dailyRollProvider);
            _ratingPopup = new RatingPopup(_ratingViewModel);

            CreateTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTrayIcon()
    {
        var iconUri = new Uri("avares://OutOfWhatApp/Assets/OOW.png");

        if (OperatingSystem.IsMacOS())
        {
            using var stream = AssetLoader.Open(iconUri);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            MacTrayIcon.TryCreate(
                memoryStream.ToArray(),
                onClick: (centerX, menuBarThickness) => Dispatcher.UIThread.Post(() =>
                {
                    // centerX doesn't need flipping (Avalonia and AppKit share the
                    // same X axis for the primary screen), and menuBarThickness is
                    // already "points from the top of the screen," which is exactly
                    // Avalonia's Y convention — no coordinate flip needed here.
                    var iconPosition = new PixelPoint((int)centerX, (int)menuBarThickness);
                    _ = ShowRatingPopupAsync(iconPosition);
                }));
            return;
        }

        var icon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(iconUri)),
            ToolTipText = "Out Of What",
        };
        icon.Clicked += (_, _) => _ = ShowRatingPopupAsync(iconPosition: null);
        TrayIcon.SetIcons(this, new TrayIcons { icon });
    }

    private async System.Threading.Tasks.Task ShowRatingPopupAsync(PixelPoint? iconPosition)
    {
        await _ratingViewModel.LoadTodayAsync();

        if (iconPosition is { } position)
        {
            PositionUnderIcon(_ratingPopup, position);
        }
        else
        {
            PositionNearTrayCorner(_ratingPopup);
        }

        _ratingPopup.Opacity = 0;
        _ratingPopup.Show();
        _ratingPopup.Activate();
        _ratingPopup.Opacity = 1; // animated by the window's own Opacity Transition
    }

    private static void PositionUnderIcon(RatingPopup window, PixelPoint iconPosition)
    {
        var screen = window.Screens?.Primary;
        if (screen is null)
        {
            return;
        }

        const int margin = 12;
        const int verticalPad = 2; // small gap so the arrow tip sits right at the icon, not overlapping it
        const double arrowInset = 35; // keep the arrow tip within the card's rounded corners (20px radius + 15px half-width)
        var area = screen.WorkingArea;
        var width = (int)window.Width;

        var x = iconPosition.X - width / 2;
        var minX = area.X + margin;
        var maxX = area.X + area.Width - width - margin;
        x = Math.Clamp(x, minX, Math.Max(minX, maxX));

        // Anchor to the menu bar's actual bottom edge rather than a fixed offset
        // from the working area, so the arrow tip lands right at the icon.
        var y = Math.Max(screen.Bounds.Y, iconPosition.Y + verticalPad);

        window.Position = new PixelPoint(x, y);

        double arrowOffsetX = iconPosition.X - x;
        arrowOffsetX = Math.Clamp(arrowOffsetX, arrowInset, width - arrowInset);
        window.UpdateShape(arrowOffsetX);
    }

    private static void PositionNearTrayCorner(RatingPopup window)
    {
        var workingArea = window.Screens?.Primary?.WorkingArea;
        if (workingArea is null)
        {
            return;
        }

        const int margin = 12;
        var area = workingArea.Value;
        var width = (int)window.Width;
        var height = (int)window.Height;

        int x;
        int y;

        if (OperatingSystem.IsMacOS())
        {
            // macOS status bar icons live top-right.
            x = area.X + area.Width - width - margin;
            y = area.Y + margin;
        }
        else
        {
            // Windows/most Linux DEs put the tray in the bottom-right.
            x = area.X + area.Width - width - margin;
            y = area.Y + area.Height - height - margin;
        }

        window.Position = new PixelPoint(x, y);
        window.UpdateShape(width / 2.0);
    }
}
