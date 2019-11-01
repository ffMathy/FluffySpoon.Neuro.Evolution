using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public delegate double CalculateFitnessOfModelDelegate<TModel>(TModel model);

    public interface IEvolutionSettings<TModel> : INeuralNetworkSettings
    {
        Random RandomnessProvider { get; }

        int? BadGenerationsToRemove { get; }

        CalculateFitnessOfModelDelegate<TModel> FitnessCalculationFunction { get; }
    }

    public interface INeuralNetworkSettings
    {
        double NeuronMutationProbability { get; }
        int[] NeuronCounts { get; }
    }
}
