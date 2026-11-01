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

        /// <summary>
        /// How many ticks a car may go without reaching new progress before it is considered stuck.
        /// </summary>
        private const int MaximumTicksWithoutProgress = 150;

        /// <summary>
        /// The simulation ends once a car has driven this many laps.
        /// </summary>
        private const int LapsToComplete = 1;

        /// <summary>
        /// How much progress per tick counts compared to progress. A car at top speed makes roughly one progress line per tick.
        /// Much higher than this, and crashing quickly into the first corner beats learning to get around it.
        /// </summary>
        private const double ProgressPerTickWeight = 0.5;

        /// <summary>
        /// Sensor distances are divided by this value so that the network receives inputs roughly in the range [0, 1].
        /// </summary>
        private const float SensorNormalizationDistance = (float)Map.TileSize * Map.SensorRangeInTiles;

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
        /// Lower is better.
        /// </summary>
        public double Fitness => CalculateFitness(Progress, TicksSurvived);

        /// <summary>
        /// Cars are ranked by how far they got along the track, multiplied by how fast they got there.
        /// Multiplying means that speed can't make up for not getting anywhere, and that going backwards is never rewarded.
        /// </summary>
        public static double CalculateFitness(int progress, long ticks)
        {
            if (progress <= 0)
                return -progress;

            var progressPerTick = ticks == 0 ? 0 : progress / (double)ticks;
            return -progress * (1 + (progressPerTick * ProgressPerTickWeight));
        }

        public CarSimulation(
            Map map)
        {
            Car = new Car();

            _map = map;

            _progressLineCount = _map.ProgressLineCount;

            HasEnded = false;
            TicksSurvived = 0;
            _laps = 0;

            CurrentProgressLine = _map.FirstProgressLine;
        }

        private ProgressLine GetClosestIntersectionPointProgressLine()
        {
            var center = Car.BoundingBox.Center;
            var closest = CurrentProgressLine;
            var closestDistance = float.MaxValue;

            foreach (var node in new[] { CurrentMapNode.Previous, CurrentMapNode, CurrentMapNode.Next })
            {
                foreach (var progressLine in node.ProgressLines)
                {
                    var distance = DistanceHelper
                        .FindClosestPointOnLine(center, progressLine.Line)
                        .GetDistanceTo(center);

                    if (distance < closestDistance)
                    {
                        closest = progressLine;
                        closestDistance = distance;
                    }
                }
            }

            return closest;
        }

        private CarSensorReadingSnapshot GetSensorReadings()
        {
            var mapLines = _map.GetWallLinesInSensorRange(Car.BoundingBox.Center);
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
            Line[] lines,
            float angleInDegrees)
        {
            var forwardDirectionLine = new Line()
            {
                Start = Car.BoundingBox.Center,
                End = Car.ForwardDirectionLine.End * 2 + Car.BoundingBox.Center
            };
            var sensorLine = forwardDirectionLine.Rotate(angleInDegrees);

            CarSensorReading? closestReading = null;
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
                if (closestReading == null || distance < closestReading.Value.Distance)
                {
                    closestReading = new CarSensorReading()
                    {
                        IntersectionPoint = intersectionPoint,
                        Distance = distance
                    };
                }
            }

            if (closestReading == null)
            {
                // The ray escaped through a gap in the walls (e.g. a corner) or found nothing within range. Treat it as "nothing in sight".
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

            return closestReading.Value;
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

            var previousCenter = Car.BoundingBox.Center;

            Car.Tick();

            if (HasCrossedWall(previousCenter, Car.BoundingBox.Center))
            {
                HasEnded = true;
                return;
            }

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

        /// <summary>
        /// Checks the whole movement of a tick rather than where it ended, since a fast car can get past a wall in one tick.
        /// </summary>
        private bool HasCrossedWall(Point from, Point to)
        {
            var movement = new Line()
            {
                Start = from,
                End = to
            };

            foreach (var wallLine in _map.GetWallLinesInSensorRange(from))
            {
                if (movement.IsCrossing(wallLine))
                    return true;
            }

            return false;
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
