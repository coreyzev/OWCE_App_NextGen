using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace OWCE.Views.Controls;

/// <summary>
/// Custom SkiaSharp control that renders the speed arc gauge.
/// Displays current speed as a filled arc, with top speed as a tick mark.
///
/// Replaces the original SpeedArcView.cs from the Xamarin app.
/// Uses SkiaSharp.Views.Maui for MAUI compatibility.
/// </summary>
public class SpeedArcView : SKCanvasView
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public static readonly BindableProperty CurrentSpeedProperty =
        BindableProperty.Create(nameof(CurrentSpeed), typeof(float), typeof(SpeedArcView), 0f,
            propertyChanged: (b, _, _) => ((SpeedArcView)b).InvalidateSurface());

    public static readonly BindableProperty TopSpeedProperty =
        BindableProperty.Create(nameof(TopSpeed), typeof(float), typeof(SpeedArcView), 0f,
            propertyChanged: (b, _, _) => ((SpeedArcView)b).InvalidateSurface());

    public static readonly BindableProperty MaxSpeedProperty =
        BindableProperty.Create(nameof(MaxSpeed), typeof(float), typeof(SpeedArcView), 30f,
            propertyChanged: (b, _, _) => ((SpeedArcView)b).InvalidateSurface());

    public static readonly BindableProperty IsRegenProperty =
        BindableProperty.Create(nameof(IsRegen), typeof(bool), typeof(SpeedArcView), false,
            propertyChanged: (b, _, _) => ((SpeedArcView)b).InvalidateSurface());

    public float CurrentSpeed
    {
        get => (float)GetValue(CurrentSpeedProperty);
        set => SetValue(CurrentSpeedProperty, value);
    }

    public float TopSpeed
    {
        get => (float)GetValue(TopSpeedProperty);
        set => SetValue(TopSpeedProperty, value);
    }

    public float MaxSpeed
    {
        get => (float)GetValue(MaxSpeedProperty);
        set => SetValue(MaxSpeedProperty, value);
    }

    public bool IsRegen
    {
        get => (bool)GetValue(IsRegenProperty);
        set => SetValue(IsRegenProperty, value);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    // Arc spans from 150° to 390° (240° total sweep), starting at bottom-left
    private const float StartAngleDeg = 150f;
    private const float SweepAngleDeg = 240f;

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var info = e.Info;
        float size = Math.Min(info.Width, info.Height);
        float cx = info.Width / 2f;
        float cy = info.Height / 2f;
        float radius = size * 0.42f;
        float strokeWidth = size * 0.06f;

        var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);

        // ── Background track ──────────────────────────────────────────────────
        using var trackPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(255, 255, 255, 30),
            StrokeWidth = strokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        };
        using var trackPath = new SKPath();
        trackPath.AddArc(rect, StartAngleDeg, SweepAngleDeg);
        canvas.DrawPath(trackPath, trackPaint);

        // ── Speed arc ─────────────────────────────────────────────────────────
        float speedRatio = Math.Clamp(CurrentSpeed / Math.Max(MaxSpeed, 1f), 0f, 1f);
        float speedSweep = SweepAngleDeg * speedRatio;

        if (speedSweep > 0)
        {
            var arcColor = IsRegen
                ? new SKColor(52, 152, 219)    // Blue for regen
                : new SKColor(0, 180, 216);    // OWCE cyan for normal

            using var arcPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = arcColor,
                StrokeWidth = strokeWidth,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
            };
            using var arcPath = new SKPath();
            arcPath.AddArc(rect, StartAngleDeg, speedSweep);
            canvas.DrawPath(arcPath, arcPaint);
        }

        // ── Top speed tick mark ───────────────────────────────────────────────
        if (TopSpeed > 0 && TopSpeed <= MaxSpeed)
        {
            float topSpeedRatio = Math.Clamp(TopSpeed / MaxSpeed, 0f, 1f);
            float topSpeedAngle = StartAngleDeg + (SweepAngleDeg * topSpeedRatio);
            float angleRad = topSpeedAngle * (float)(Math.PI / 180.0);

            float innerR = radius - strokeWidth * 0.8f;
            float outerR = radius + strokeWidth * 0.8f;

            float x1 = cx + innerR * (float)Math.Cos(angleRad);
            float y1 = cy + innerR * (float)Math.Sin(angleRad);
            float x2 = cx + outerR * (float)Math.Cos(angleRad);
            float y2 = cy + outerR * (float)Math.Sin(angleRad);

            using var tickPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(231, 76, 60),  // Red tick for top speed
                StrokeWidth = strokeWidth * 0.4f,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
            };
            canvas.DrawLine(x1, y1, x2, y2, tickPaint);
        }
    }
}
