using System;
using System.Collections.Generic;
using System.Linq;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    /// <summary>
    /// Uses a PID controller to pick how many generations to train at a time, so that a batch takes about
    /// as long as a given target. Runs get longer as cars learn to survive, so only recent runs count.
    /// Run times differ by orders of magnitude, so the controller works on logarithms. That makes
    /// being twice as slow as the target the same error no matter how many generations are involved.
    /// </summary>
    public class GenerationsPerBatchController
    {
        public const int MinimumGenerationsPerBatch = 1;
        public const int MaximumGenerationsPerBatch = 10000;

        private const int RunsToAverage = 20;

        private const double ProportionalGain = 0.2;
        private const double IntegralGain = 0.6;
        private const double DerivativeGain = 0.02;

        private static readonly TimeSpan MinimumTargetTrainingTime = TimeSpan.FromSeconds(0.5);
        private static readonly TimeSpan MinimumRunTime = TimeSpan.FromMilliseconds(0.1);

        private readonly Queue<TimeSpan> _runTimes = new();

        private readonly PidController _pidController = new(
            ProportionalGain,
            IntegralGain,
            DerivativeGain,
            Math.Log(MinimumGenerationsPerBatch),
            Math.Log(MaximumGenerationsPerBatch));

        private int? _lastGenerationsPerBatch;

        public TimeSpan? AverageRunTime => _runTimes.Count == 0
            ? null
            : TimeSpan.FromTicks((long)_runTimes.Average(x => x.Ticks));

        public void AddRun(TimeSpan runTime)
        {
            _runTimes.Enqueue(runTime);

            if (_runTimes.Count > RunsToAverage)
                _runTimes.Dequeue();
        }

        /// <summary>
        /// Forgets the run times, which belong to a specific map. The controller keeps its state, and treats the
        /// runs on a new map as any other change in run time.
        /// </summary>
        public void ForgetRuns()
        {
            _runTimes.Clear();
        }

        /// <summary>
        /// Returns how many generations the next batch should have to take the target time, given how many the current one has.
        /// A different amount than last returned means it was typed in, so the controller continues from there.
        /// </summary>
        public int GetNextGenerationsPerBatch(int currentGenerationsPerBatch, TimeSpan targetTrainingTime)
        {
            var averageRunTime = AverageRunTime;
            if (averageRunTime == null)
                return currentGenerationsPerBatch;

            if (currentGenerationsPerBatch != _lastGenerationsPerBatch)
                _pidController.Reset(Math.Log(currentGenerationsPerBatch));

            var runTime = TimeSpan.FromTicks(Math.Max(averageRunTime.Value.Ticks, MinimumRunTime.Ticks));
            var expectedTrainingTime = runTime * currentGenerationsPerBatch;
            var target = targetTrainingTime > MinimumTargetTrainingTime ? targetTrainingTime : MinimumTargetTrainingTime;
            var error = Math.Log(target / expectedTrainingTime);

            _lastGenerationsPerBatch = (int)Math.Clamp(
                Math.Round(Math.Exp(_pidController.Update(error))),
                MinimumGenerationsPerBatch,
                MaximumGenerationsPerBatch);

            return _lastGenerationsPerBatch.Value;
        }
    }
}
