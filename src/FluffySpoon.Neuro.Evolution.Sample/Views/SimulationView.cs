using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FluffySpoon.Neuro.Evolution.Sample.Models;
using Point = Avalonia.Point;
using Size = Avalonia.Size;

namespace FluffySpoon.Neuro.Evolution.Sample.Views;

/// <summary>
/// Draws the map and the cars directly, instead of creating a control per shape every tick.
/// The map never changes while it is shown, so it is drawn once into a bitmap.
/// The map is centered and scaled down to fit, without changing the size of anything in the simulation.
/// </summary>
public class SimulationView : Control
{
    private const int MaximumFramesPerSecond = 60;

    /// <summary>
    /// How small a tile may be drawn. Any smaller and the cars get hard to tell apart.
    /// </summary>
    private const double MinimumTileViewSize = 60;

    /// <summary>
    /// Below this scale, the progress line labels overlap each other.
    /// </summary>
    private const double MinimumLabelScale = 0.75;

    private const double ViewMargin = 12;

    private const int FinishLineSquareCount = 8;

    /// <summary>
    /// A hidden window may not get drawn at all, which must not stop the training.
    /// </summary>
    private static readonly TimeSpan MaximumFrameWait = TimeSpan.FromMilliseconds(100);

    private static readonly TimeSpan FrameInterval = TimeSpan.FromSeconds(1d / MaximumFramesPerSecond);

    private static readonly IBrush MapNodeBrush = new ImmutableSolidColorBrush(Colors.White);
    private static readonly IBrush LabelBrush = new ImmutableSolidColorBrush(Colors.Black, 0.25);
    private static readonly IBrush FinishLineLabelBrush = new ImmutableSolidColorBrush(Colors.Black);
    private static readonly IPen ProgressLinePen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.LightGray, 0.25), 1);
    private static readonly IPen WallLinePen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.DimGray), 2);

    private static readonly IPen DrivingCarPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.Green), 2);
    private static readonly IPen EndedCarPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.Red), 2);
    private static readonly IPen BestCarPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.Blue), 2);
    private static readonly IPen ForwardLinePen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.Blue), 1.5);
    private static readonly IPen SensorLinePen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.Blue, 0.2), 1);

    private readonly ManualResetEventSlim _carsDrawn = new(false);
    private readonly Stopwatch _frameStopwatch = Stopwatch.StartNew();

    private TimeSpan _nextFrameTime;

    private volatile CarDrawing[] _cars = Array.Empty<CarDrawing>();
    private CarDrawing[]? _lastDrawnCars;

    private Map? _map;
    private Rect _mapBounds;

    private RenderTargetBitmap? _mapBitmap;
    private double _mapBitmapScale;
    private double _mapBitmapRenderScaling;

    private double _scale;
    private Point _origin;

    /// <summary>
    /// Raised on the UI thread each time new cars have been drawn.
    /// </summary>
    public event Action? FrameDrawn;

    /// <summary>
    /// How many tiles a map can have in each direction, and still fit with its tiles at a reasonable size.
    /// </summary>
    public (int Columns, int Rows) MaximumMapSize => (
        (int)((Bounds.Width - (ViewMargin * 2)) / MinimumTileViewSize),
        (int)((Bounds.Height - (ViewMargin * 2)) / MinimumTileViewSize));

    public void SetMap(Map map)
    {
        var boxes = map.Nodes.Select(x => x.BoundingBox).ToArray();
        var left = boxes.Min(x => x.Location.X);
        var top = boxes.Min(x => x.Location.Y);

        _map = map;
        _mapBounds = new Rect(
            left,
            top,
            boxes.Max(x => x.Location.X + x.Size.Width) - left,
            boxes.Max(x => x.Location.Y + x.Size.Height) - top);
        _cars = Array.Empty<CarDrawing>();

        _mapBitmap?.Dispose();
        _mapBitmap = null;

        InvalidateVisual();
    }

    /// <summary>
    /// Whether a frame can be drawn right now without exceeding the frame rate.
    /// </summary>
    public bool IsFrameDue => _frameStopwatch.Elapsed >= _nextFrameTime;

    /// <summary>
    /// Called on the training thread. Blocks until the cars have been drawn, so that every tick is shown
    /// instead of ticks piling up faster than they can be drawn, and no sooner than the frame rate allows.
    /// </summary>
    public void DrawCars(CarDrawing[] cars)
    {
        WaitForNextFrameTime();

        _carsDrawn.Reset();
        _cars = cars;
        Dispatcher.UIThread.Post(InvalidateVisual);

        _carsDrawn.Wait(MaximumFrameWait);
    }

    /// <summary>
    /// Called on the training thread when ticks are not to be held back, which leaves out the ticks between frames.
    /// </summary>
    public void DrawCarsWithoutWaiting(CarDrawing[] cars)
    {
        WaitForNextFrameTime();

        _cars = cars;
        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    /// <summary>
    /// Frame times are spaced evenly from the previous frame time rather than from when the previous frame
    /// was drawn, since waiting for the renderer's next tick would otherwise add up and lower the frame rate.
    /// Sleeping is too coarse for this, and the wait is short since most of the interval is spent drawing.
    /// </summary>
    private void WaitForNextFrameTime()
    {
        var spinWait = new SpinWait();
        while (_frameStopwatch.Elapsed < _nextFrameTime)
            spinWait.SpinOnce(-1);

        var now = _frameStopwatch.Elapsed;
        var hasFallenBehind = now - _nextFrameTime > FrameInterval;
        _nextFrameTime = (hasFallenBehind ? now : _nextFrameTime) + FrameInterval;
    }

    public override void Render(DrawingContext context)
    {
        if (_map == null)
            return;

        var renderScaling = VisualRoot?.RenderScaling ?? 1;

        _scale = Math.Clamp(
            Math.Min(
                (Bounds.Width - (ViewMargin * 2)) / _mapBounds.Width,
                (Bounds.Height - (ViewMargin * 2)) / _mapBounds.Height),
            0.01,
            1);

        // Whole device pixels keep the bitmap from being blurred by interpolation.
        var left = Math.Round((Bounds.Width - (_mapBounds.Width * _scale)) / 2 * renderScaling) / renderScaling;
        var top = Math.Round((Bounds.Height - (_mapBounds.Height * _scale)) / 2 * renderScaling) / renderScaling;
        _origin = new Point(left - (_mapBounds.X * _scale), top - (_mapBounds.Y * _scale));

        if (_mapBitmap == null || _mapBitmapScale != _scale || _mapBitmapRenderScaling != renderScaling)
            RenderMapBitmap(_map, renderScaling);

        var margin = WallLinePen.Thickness;
        context.DrawImage(
            _mapBitmap!,
            new Rect(0, 0, _mapBitmap!.PixelSize.Width, _mapBitmap.PixelSize.Height),
            new Rect(new Point(left - margin, top - margin), _mapBitmap.Size));

        var cars = _cars;
        foreach (var car in cars)
            DrawCar(context, car);

        if (ReferenceEquals(cars, _lastDrawnCars))
            return;

        _lastDrawnCars = cars;
        FrameDrawn?.Invoke();
        _carsDrawn.Set();
    }

    private void RenderMapBitmap(Map map, double renderScaling)
    {
        var margin = WallLinePen.Thickness;
        var size = new Size(
            (_mapBounds.Width * _scale) + (margin * 2),
            (_mapBounds.Height * _scale) + (margin * 2));

        _mapBitmap?.Dispose();
        _mapBitmap = new RenderTargetBitmap(
            new PixelSize(
                (int)Math.Ceiling(size.Width * renderScaling),
                (int)Math.Ceiling(size.Height * renderScaling)),
            new Vector(96 * renderScaling, 96 * renderScaling));
        _mapBitmapScale = _scale;
        _mapBitmapRenderScaling = renderScaling;

        var topLeft = ToView(_mapBounds.X, _mapBounds.Y);

        using var context = new DrawingContext(_mapBitmap.CreateDrawingContext(null));
        using (context.PushPostTransform(Matrix.CreateTranslation(margin - topLeft.X, margin - topLeft.Y)))
        {
            foreach (var node in map.Nodes)
                DrawMapNode(context, node);

            DrawFinishLine(context, map.FirstProgressLine.Line);
        }
    }

    /// <summary>
    /// A checkered band across the progress line where a lap is counted, which is also where the cars start.
    /// </summary>
    private void DrawFinishLine(DrawingContext context, Line line)
    {
        var start = ToView(line.Start.X, line.Start.Y);
        var end = ToView(line.End.X, line.End.Y);

        var step = (end - start) / FinishLineSquareCount;
        var squareSize = Math.Sqrt((step.X * step.X) + (step.Y * step.Y));
        var sideways = new Point(-step.Y, step.X);

        var darkPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.Black), squareSize);
        var lightPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.White), squareSize);

        for (var row = 0; row < 2; row++)
        {
            for (var i = 0; i < FinishLineSquareCount; i++)
            {
                var from = start + (step * i) + (sideways * (row - 0.5));
                context.DrawLine((i + row) % 2 == 0 ? darkPen : lightPen, from, from + step);
            }
        }

        var label = new FormattedText("Start/finish", Typeface.Default, 9, TextAlignment.Left, TextWrapping.NoWrap, Size.Infinity);
        var center = start + ((end - start) / 2);
        context.DrawText(
            FinishLineLabelBrush,
            new Point(center.X - (label.Bounds.Width / 2), center.Y + squareSize + 1),
            label);
    }

    private Point ToView(double x, double y)
    {
        return new Point(_origin.X + (x * _scale), _origin.Y + (y * _scale));
    }

    private Point ToView(Point point)
    {
        return ToView(point.X, point.Y);
    }

    private void DrawMapNode(DrawingContext context, MapNode node)
    {
        var box = node.BoundingBox;
        context.DrawRectangle(
            MapNodeBrush,
            null,
            new Rect(
                ToView(box.Location.X, box.Location.Y),
                ToView(box.Location.X + box.Size.Width, box.Location.Y + box.Size.Height)));

        foreach (var progressLine in node.ProgressLines)
        {
            DrawLine(context, ProgressLinePen, progressLine.Line);

            if (_scale >= MinimumLabelScale)
                DrawLabel(context, progressLine.Line.Center, progressLine.Offset.ToString());
        }

        foreach (var wallLine in node.WallLines)
            DrawLine(context, WallLinePen, wallLine.Line);
    }

    private void DrawLine(DrawingContext context, IPen pen, Line line)
    {
        context.DrawLine(
            pen,
            ToView(line.Start.X, line.Start.Y),
            ToView(line.End.X, line.End.Y));
    }

    private void DrawLabel(DrawingContext context, Models.Point point, string text)
    {
        context.DrawText(
            LabelBrush,
            ToView(point.X, point.Y),
            new FormattedText(text, Typeface.Default, 10, TextAlignment.Left, TextWrapping.NoWrap, Size.Infinity));
    }

    private void DrawCar(DrawingContext context, CarDrawing car)
    {
        var pen = car.State switch
        {
            CarDrawingState.Best => BestCarPen,
            CarDrawingState.Ended => EndedCarPen,
            _ => DrivingCarPen
        };

        var center = ToView(car.Center);
        var radius = Math.Max(1, (Car.Size * _scale / 2) - (pen.Thickness / 2));
        context.DrawEllipse(null, pen, center, radius, radius);

        if (car.HasEnded)
            return;

        context.DrawLine(ForwardLinePen, center, ToView(car.ForwardEnd));

        foreach (var sensorEnd in car.SensorEnds)
            context.DrawLine(SensorLinePen, center, ToView(sensorEnd));
    }
}
