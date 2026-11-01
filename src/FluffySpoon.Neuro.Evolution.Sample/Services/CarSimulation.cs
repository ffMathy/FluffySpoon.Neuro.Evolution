using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        /// <summary>
        /// How many ticks a car may go without reaching new progress before it is considered stuck.
        /// </summary>
        private const int MaximumTicksWithoutProgress = 150;

        /// <summary>
        /// The simulation ends once a car has driven this many laps.
        /// </summary>
        private const int LapsToComplete = 2;

        /// <summary>
        /// Sensor distances are divided by this value so that the network receives inputs roughly in the range [0, 1].
        /// </summary>
        private const float SensorNormalizationDistance = (float)Map.TileSize * 3;

        private readonly int _progressLineCount;

        private int _laps;

        private long _lastProgressIncreaseTick;
        private int _highestProgress;

        public long TicksSurvived { get; set; }

        public ProgressLine CurrentProgressLine { get; set; }

        public MapNode CurrentMapNode => CurrentProgressLine.MapNode;

        public CarSensorReadingSnapshot SensorReadings { get; private set; }

        public Car Car { get; private set; }

        public bool HasEnded { get; private set; }

        /// <summary>
        /// The total amount of progress lines passed, including full laps. Can be negative when driving backwards.
        /// </summary>
        public int ProgressLineCount => _progressLineCount;

        public int Progress => (_laps * _progressLineCount) + (CurrentProgressLine.Offset - 1);

        /// <summary>
        /// Lower is better. Cars are ranked by how far they got along the track.
        /// Once a car completes all laps, the time it took becomes a tie-breaker so that faster cars win.
        /// Speed is deliberately not rewarded before that, since it would push the population towards
        /// driving fast and crashing in tight corners instead of learning to brake.
        /// </summary>
        public double Fitness => _laps >= LapsToComplete
            ? (-Progress * 100d) + (TicksSurvived / 100d)
            : -Progress * 100d;

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

            _progressLineCount = allProgressLines.Last().Offset;

            HasEnded = false;
            TicksSurvived = 0;
            _laps = 0;

            CurrentProgressLine = allProgressLines.First();
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
            {
                // The ray escaped through a gap in the walls (e.g. a corner). Treat it as "nothing in sight".
                var direction = sensorLine.End - sensorLine.Start;
                var length = direction.GetDistanceTo(new Point(0, 0));
                return new CarSensorReading()
                {
                    IntersectionPoint = length == 0
                        ? sensorLine.Start
                        : new Point(
                            sensorLine.Start.X + (direction.X * SensorNormalizationDistance / length),
                            sensorLine.Start.Y + (direction.Y * SensorNormalizationDistance / length)),
                    Distance = SensorNormalizationDistance
                };
            }

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
                NormalizeSensorDistance(SensorReadings.LeftSensor),
                NormalizeSensorDistance(SensorReadings.LeftCenterSensor),
                NormalizeSensorDistance(SensorReadings.CenterSensor),
                NormalizeSensorDistance(SensorReadings.RightCenterSensor),
                NormalizeSensorDistance(SensorReadings.RightSensor),
                (Car.SpeedVelocity - Car.MinimumSpeed) / (Car.MaximumSpeed - Car.MinimumSpeed),
                Car.TurnAngleVelocity / Car.MaximumTurnAngleVelocity
            };
        }

        private static float NormalizeSensorDistance(CarSensorReading reading)
        {
            return Math.Min(1f, reading.Distance / SensorNormalizationDistance);
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

            UpdateProgress();

            var mapNodeBoundingBoxes = new[]
            {
                CurrentMapNode.Previous.Previous.BoundingBox,
                CurrentMapNode.Previous.BoundingBox,
                CurrentMapNode.BoundingBox,
                CurrentMapNode.Next.BoundingBox,
                CurrentMapNode.Next.Next.BoundingBox
            };

            var isWithinAnyNode = Car.BoundingBox.IsWithin(mapNodeBoundingBoxes);
            if (!isWithinAnyNode)
            {
                HasEnded = true;
                return;
            }

            TicksSurvived++;

            if (Progress > _highestProgress)
            {
                _highestProgress = Progress;
                _lastProgressIncreaseTick = TicksSurvived;
            }

            if (TicksSurvived - _lastProgressIncreaseTick > MaximumTicksWithoutProgress)
            {
                HasEnded = true;
                return;
            }

            if (_laps >= LapsToComplete)
            {
                HasEnded = true;
            }
        }

        private void UpdateProgress()
        {
            var previousProgressLine = CurrentProgressLine;
            var newProgressLine = GetClosestIntersectionPointProgressLine();

            // Offsets wrap around at the finish line, so compute the shortest signed distance around the loop.
            var delta = newProgressLine.Offset - previousProgressLine.Offset;
            if (delta > _progressLineCount / 2)
                delta -= _progressLineCount;
            else if (delta < -_progressLineCount / 2)
                delta += _progressLineCount;

            // Ignore jumps; a car can only move a couple of progress lines per tick.
            if (Math.Abs(delta) >= 3)
                return;

            if (delta > 0 && newProgressLine.Offset < previousProgressLine.Offset)
                _laps++;
            else if (delta < 0 && newProgressLine.Offset > previousProgressLine.Offset)
                _laps--;

            CurrentProgressLine = newProgressLine;
        }

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
        }
    }
}
