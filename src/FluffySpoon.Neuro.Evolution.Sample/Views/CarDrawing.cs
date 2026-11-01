using System;
using Avalonia;
using FluffySpoon.Neuro.Evolution.Sample.Models;
using FluffySpoon.Neuro.Evolution.Sample.Services;
using Point = Avalonia.Point;

namespace FluffySpoon.Neuro.Evolution.Sample.Views;

public enum CarDrawingState
{
    Driving,
    Ended,
    Best
}

/// <summary>
/// A copy of what is needed to draw a car, so that drawing never reads a simulation that the training thread is changing.
/// </summary>
public class CarDrawing
{
    public Point Center { get; }
    public Point ForwardEnd { get; }
    public Point[] SensorEnds { get; }
    public bool HasEnded { get; }
    public CarDrawingState State { get; }

    public CarDrawing(CarSimulation carSimulation, bool isBest)
    {
        var car = carSimulation.Car;
        var center = car.BoundingBox.Center;
        var forwardEnd = car.ForwardDirectionLine.End;
        var sensorReadings = carSimulation.SensorReadings;

        Center = new Point(center.X, center.Y);
        ForwardEnd = new Point(
            center.X + (forwardEnd.X * (double)car.BoundingBox.Size.Width),
            center.Y + (forwardEnd.Y * (double)car.BoundingBox.Size.Height));
        HasEnded = carSimulation.HasEnded;
        SensorEnds = HasEnded
            ? Array.Empty<Point>()
            : new[]
            {
                ToPoint(sensorReadings.LeftSensor),
                ToPoint(sensorReadings.LeftCenterSensor),
                ToPoint(sensorReadings.CenterSensor),
                ToPoint(sensorReadings.RightCenterSensor),
                ToPoint(sensorReadings.RightSensor)
            };

        if (isBest)
            State = CarDrawingState.Best;
        else
            State = HasEnded ? CarDrawingState.Ended : CarDrawingState.Driving;
    }

    private static Point ToPoint(CarSensorReading reading) => new(
        reading.IntersectionPoint.X,
        reading.IntersectionPoint.Y);
}
