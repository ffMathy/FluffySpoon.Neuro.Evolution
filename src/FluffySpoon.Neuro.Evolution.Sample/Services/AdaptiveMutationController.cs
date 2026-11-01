using System;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    /// <summary>
    /// Mutates gently while the best fitness keeps improving, so that what works gets fine-tuned,
    /// and increasingly boldly the longer it doesn't, so that the population can get out of a dead end.
    /// </summary>
    public class AdaptiveMutationController
    {
        private const float GentlestProbability = 0.05f;
        private const float BoldestProbability = 0.15f;

        private const float GentlestStrength = 0.1f;
        private const float BoldestStrength = 0.5f;

        private const int GenerationsUntilBoldest = 100;

        /// <summary>
        /// Starting in the middle, since nothing is known about how training is going yet.
        /// </summary>
        private const int InitialGenerationsWithoutImprovement = GenerationsUntilBoldest / 2;

        /// <summary>
        /// How much better fitness must get to count as an improvement. Tiny gains happen all the time, even in a dead end.
        /// </summary>
        private const double MinimumRelativeImprovement = 0.005;

        private double? _bestFitness;
        private int _generationsWithoutImprovement = InitialGenerationsWithoutImprovement;

        private float Boldness => Math.Min(1f, _generationsWithoutImprovement / (float)GenerationsUntilBoldest);

        public float MutationProbability => GentlestProbability + (Boldness * (BoldestProbability - GentlestProbability));

        public float MutationStrength => GentlestStrength + (Boldness * (BoldestStrength - GentlestStrength));

        /// <param name="bestFitness">The best fitness of the generation. Lower is better.</param>
        public void AddGeneration(double bestFitness)
        {
            if (_bestFitness == null)
            {
                _bestFitness = bestFitness;
                return;
            }

            if (bestFitness < _bestFitness - (Math.Abs(_bestFitness.Value) * MinimumRelativeImprovement))
            {
                _bestFitness = bestFitness;
                _generationsWithoutImprovement = 0;
                return;
            }

            _generationsWithoutImprovement++;
        }

        /// <summary>
        /// Fitness on one map says nothing about fitness on another, so a new map starts over.
        /// </summary>
        public void ForgetProgress()
        {
            _bestFitness = null;
            _generationsWithoutImprovement = InitialGenerationsWithoutImprovement;
        }
    }
}
