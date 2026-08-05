using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OutOfWhatApp.ViewModels;

namespace OutOfWhatApp.Views;

public partial class RatingPopup : Window
{
    private const double CardWidth = 240;
    private const double CardHeight = 320;
    private const double ArrowHeight = 20;
    private const double ArrowHalfWidth = 15;
    private const double ArrowTipPeakY = 3; // how close the rounded tip comes to y=0
    private const double ArrowCornerRadius = 20; // brand token: RadiusPopup

    public RatingPopup()
    {
        InitializeComponent();
        UpdateShape(CardWidth / 2);
    }

    public RatingPopup(RatingViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.Saved += (_, _) => Hide();
        Deactivated += (_, _) => Hide();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Keep the app alive in the tray instead of exiting when the popup closes.
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }

    // Traces the whole bubble — rounded-rect card plus a pointed notch cut into
    // its top edge at arrowOffsetX — as one continuous outline, so the arrow and
    // card render as a single filled/stroked shape instead of two seamed pieces.
    public void UpdateShape(double arrowOffsetX)
    {
        arrowOffsetX = Math.Clamp(arrowOffsetX, ArrowCornerRadius + ArrowHalfWidth, CardWidth - ArrowCornerRadius - ArrowHalfWidth);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            // The control point sits above y=0 so the curve's peak (at t=0.5) lands
            // at ArrowTipPeakY — a wide, rounded arch instead of a sharp point.
            var controlY = (2 * ArrowTipPeakY) - ArrowHeight;

            ctx.BeginFigure(new Point(ArrowCornerRadius, ArrowHeight), isFilled: true);
            ctx.LineTo(new Point(arrowOffsetX - ArrowHalfWidth, ArrowHeight));
            ctx.QuadraticBezierTo(new Point(arrowOffsetX, controlY), new Point(arrowOffsetX + ArrowHalfWidth, ArrowHeight));
            ctx.LineTo(new Point(CardWidth - ArrowCornerRadius, ArrowHeight));
            ctx.ArcTo(new Point(CardWidth, ArrowHeight + ArrowCornerRadius), new Size(ArrowCornerRadius, ArrowCornerRadius), 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(CardWidth, ArrowHeight + CardHeight - ArrowCornerRadius));
            ctx.ArcTo(new Point(CardWidth - ArrowCornerRadius, ArrowHeight + CardHeight), new Size(ArrowCornerRadius, ArrowCornerRadius), 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(ArrowCornerRadius, ArrowHeight + CardHeight));
            ctx.ArcTo(new Point(0, ArrowHeight + CardHeight - ArrowCornerRadius), new Size(ArrowCornerRadius, ArrowCornerRadius), 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(0, ArrowHeight + ArrowCornerRadius));
            ctx.ArcTo(new Point(ArrowCornerRadius, ArrowHeight), new Size(ArrowCornerRadius, ArrowCornerRadius), 0, false, SweepDirection.Clockwise);
            ctx.EndFigure(isClosed: true);
        }

        OutlinePath.Data = geometry;
    }
}
