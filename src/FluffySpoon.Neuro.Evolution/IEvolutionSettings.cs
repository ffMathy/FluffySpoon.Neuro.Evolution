using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public delegate double CalculateFitnessOfModelDelegate<TModel>(TModel model);

    public interface IEvolutionSettings<TModel>
    {
        Random RandomnessProvider { get; }

        double NeuronMutationProbability { get; }
        int? BadGenerationsToRemove { get; }

        CalculateFitnessOfModelDelegate<TModel> FitnessCalculationFunction { get; }
    }
}
