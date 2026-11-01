using System;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    /// <summary>
    /// Uses a PID controller to pick how many genomes a generation has, so that as many cars as possible are
    /// trained and drawn while the frame rate stays at the target. Since the frame rate is capped above the target,
    /// the count keeps growing until drawing starts to slow down. Like <see cref="GenerationsPerBatchController"/>,
    /// this works on logarithms, so that being 10% off means the same at any amount of genomes.
    /// </summary>
    public class GenomeCountController
    {
        public const int InitialGenomeCount = 125;
        public const int MinimumGenomeCount = 10;
        public const int MaximumGenomeCount = 2000;

        private const double TargetFramesPerSecond = 50;

        /// <summary>
        /// The share of genomes that survive each generation.
        /// </summary>
        private const double SurvivalRate = 0.2;

        private const double ProportionalGain = 0.5;
        private const double IntegralGain = 0.2;
        private const double DerivativeGain = 0.05;

        private readonly PidController _pidController = new(
            ProportionalGain,
            IntegralGain,
            DerivativeGain,
            Math.Log(MinimumGenomeCount),
            Math.Log(MaximumGenomeCount));

        public GenomeCountController()
        {
            _pidController.Reset(Math.Log(InitialGenomeCount));
        }

        public int GetNextGenomeCount(double framesPerSecond)
        {
            var error = Math.Log(Math.Max(framesPerSecond, 1) / TargetFramesPerSecond);
            return (int)Math.Round(Math.Exp(_pidController.Update(error)));
        }

        public static int GetGenomesToRemove(int genomeCount)
        {
            return genomeCount - Math.Max(1, (int)Math.Round(genomeCount * SurvivalRate));
        }
    }
}
