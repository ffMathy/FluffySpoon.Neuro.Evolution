using System;
using System.Collections.Generic;
using System.Linq;
using FluffySpoon.Neuro.Evolution.Domain;
using FluffySpoon.Neuro.Evolution.Sample.Helpers;
using FluffySpoon.Neuro.Evolution.Sample.Models;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    public struct CarResponse
    {
        public float AccelerationDeltaVelocity { get; set; }
        public float TurnDeltaAngle { get; set; }
    }

    public struct CarSensorReadingSnapshot
    {
        public CarSensorReading LeftCenterSensor { get; set; }
        public CarSensorReading CenterSensor { get; set; }
        public CarSensorReading RightCenterSensor { get; set; }
        public CarSensorReading LeftSensor { get; set; }
        public CarSensorReading RightSensor { get; set; }
    }

    public struct CarSensorReading
    {
        public Point IntersectionPoint { get; set; }
        public float Distance { get; set; }
    }

    public class CarSimulation : ISimulation
    {
        private readonly Map _map;

        private readonly IDictionary<int, List<ProgressLine>> _allProgressLinesByMapNodeOffset;

        private int _laps;
        private int _lastProgressLineOffset;

        private long _lastProgressLineIncreaseTick;
        private int _highestProgressLineOffset;

        public long TicksSurvived { get; set; }

        public ProgressLine CurrentProgressLine { get; set; }

        public MapNode CurrentMapNode => CurrentProgressLine.MapNode;

        public CarSensorReadingSnapshot SensorReadings { get; private set; }

        public Car Car { get; private set; }

        public bool HasEnded { get; private set; }

        public double Fitness
        {
            get
            {
                var mapNodesLength = _map.Nodes.Length;

                var progressPenalty = mapNodesLength - CurrentProgressLine.Offset;
                var lapPenalty = mapNodesLength * _laps;

                var timePenalty = -TicksSurvived * 10;

                return timePenalty + ((progressPenalty - lapPenalty) * 30);
            }
        }

        public CarSimulation(
            Map map)
        {
            Car = new Car();

            _map = map;

            var allProgressLines = _map.Nodes
                .SelectMany(x => x.ProgressLines)
                .OrderBy(x => x.Offset)
                .ToArray();
            _allProgressLinesByMapNodeOffset = allProgressLines
                .GroupBy(x => x
                    .MapNode
                    .Offset)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList());

            _lastProgressLineOffset = allProgressLines.Last().Offset;

            HasEnded = false;
            TicksSurvived = 0;
            _laps = 0;

            CurrentProgressLine = _map.Nodes.First().ProgressLines.First();
        }

        private ProgressLine GetClosestIntersectionPointProgressLine()
        {
            var before = _allProgressLinesByMapNodeOffset[CurrentMapNode.Previous.Offset];
            var current = _allProgressLinesByMapNodeOffset[CurrentMapNode.Offset];
            var after = _allProgressLinesByMapNodeOffset[CurrentMapNode.Next.Offset];

            var lines = before.Union(current).Union(after);

            return lines
                .OrderBy(progressLine => DistanceHelper
                    .FindClosestPointOnLine(
                        Car.BoundingBox.Center,
                        progressLine.Line)
                    .GetDistanceTo(Car.BoundingBox.Center))
                .First();
        }

        private CarSensorReadingSnapshot GetSensorReadings()
        {
            var mapLines = _map.Nodes
                .SelectMany(x => x.WallLines)
                .Select(x => x.Line)
                .ToArray();
            var sensorReadingCached = new CarSensorReadingSnapshot()
            {
                LeftSensor = GetSensorReading(mapLines, -360f / 16f),
                LeftCenterSensor = GetSensorReading(mapLines, -360f / 8f),
                CenterSensor = GetSensorReading(mapLines, 0f),
                RightCenterSensor = GetSensorReading(mapLines, 360f / 8f),
                RightSensor = GetSensorReading(mapLines, 360f / 16f)
            };

            return sensorReadingCached;
        }

        private CarSensorReading GetSensorReading(
            IEnumerable<Line> lines,
            float angleInDegrees)
        {
            var forwardDirectionLine = new Line()
            {
                Start = Car.BoundingBox.Center,
                End = Car.ForwardDirectionLine.End * 2 + Car.BoundingBox.Center
            };
            var sensorLine = forwardDirectionLine.Rotate(angleInDegrees);

            var carSensorReadings = new HashSet<CarSensorReading>();
            foreach (var line in lines)
            {
                var intersectionPointNullable = sensorLine.GetIntersectionPointWith(line);
                if (intersectionPointNullable == null)
                    continue;

                var intersectionPoint = intersectionPointNullable.Value;
                if (DirectionHelper.IsPointOutsideLineBoundaries(line, intersectionPoint))
                    continue;

                if (!DirectionHelper.IsPointInDirectionOfSensorLine(sensorLine, intersectionPoint))
                    continue;

                var distance = Car.BoundingBox.Center.GetDistanceTo(intersectionPoint);
                carSensorReadings.Add(new CarSensorReading()
                {
                    IntersectionPoint = intersectionPoint,
                    Distance = distance
                });
            }

            if (carSensorReadings.Count == 0)
                throw new InvalidOperationException("No sensor reading could be made.");

            return carSensorReadings.MinBy(x => x.Distance);
        }

        private static float NormalizeBinaryPrediction(float prediction)
        {
            if (prediction > 0.66)
            {
                return 1;
            }
            else if (prediction < 0.33)
            {
                return -1;
            }

            return 0;
        }

        public float[] GetInputs()
        {
            SensorReadings = GetSensorReadings();
            return new float[]
            {
                SensorReadings.LeftSensor.Distance,
                SensorReadings.LeftCenterSensor.Distance,
                SensorReadings.CenterSensor.Distance,
                SensorReadings.RightCenterSensor.Distance,
                SensorReadings.RightSensor.Distance,
                Car.SpeedVelocity,
                Car.TurnAngle
            };
        }

        public void Tick(float[] outputs)
        {
            var neuralNetCarResponse = new CarResponse()
            {
                AccelerationDeltaVelocity = NormalizeBinaryPrediction(outputs[0]),
                TurnDeltaAngle = NormalizeBinaryPrediction(outputs[1])
            };

            var deltaVelocity = neuralNetCarResponse.AccelerationDeltaVelocity;
            var deltaAngle = neuralNetCarResponse.TurnDeltaAngle;

            Car.Accelerate(deltaVelocity);
            Car.Turn(deltaAngle);

            Car.Tick();

            var previousProgressLine = CurrentProgressLine;
            var newProgressLine = GetClosestIntersectionPointProgressLine();
            if (previousProgressLine == null || Math.Abs(newProgressLine.Offset - previousProgressLine.Offset) < 3)
                CurrentProgressLine = newProgressLine;

            var mapNodeBoundingBoxes = new[]
            {
                CurrentMapNode.Previous.Previous.BoundingBox,
                CurrentMapNode.Previous.BoundingBox,
                CurrentMapNode.BoundingBox,
                CurrentMapNode.Next.BoundingBox,
                CurrentMapNode.Next.Next.BoundingBox
            };

            if (newProgressLine.Offset > _highestProgressLineOffset || (newProgressLine.Offset == 0 && previousProgressLine.Offset == _lastProgressLineOffset))
            {
                _lastProgressLineIncreaseTick = TicksSurvived;
                _lastProgressLineOffset = previousProgressLine.Offset;
                _highestProgressLineOffset = newProgressLine.Offset;
            }

            CheckForNewLapCount(previousProgressLine);

            var isWithinAnyNode = Car.BoundingBox.IsWithin(mapNodeBoundingBoxes);
            if (!isWithinAnyNode)
            {
                HasEnded = true;
                return;
            }

            TicksSurvived++;

            if (_lastProgressLineIncreaseTick != 0)
            {
                var timeSinceLastProgressLineIncrease = TicksSurvived - _lastProgressLineIncreaseTick;
                if (timeSinceLastProgressLineIncrease > 600)
                {
                    HasEnded = true;
                    return;
                }
            }

            if (Fitness > 3000)
            {
                HasEnded = true;
                return;
            }
        }

        private void CheckForNewLapCount(ProgressLine previousProgressLine)
        {
            if (previousProgressLine == null)
            {
                return;
            }

            if (CurrentProgressLine.Offset == 0 && previousProgressLine.Offset == _lastProgressLineOffset)
            {
                _laps++;
            }
            else if (previousProgressLine.Offset == 0 && CurrentProgressLine.Offset == _lastProgressLineOffset)
            {
                _laps--;
            }
        }
    }
}
