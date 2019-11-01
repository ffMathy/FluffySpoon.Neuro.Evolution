using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public delegate double CalculateFitnessOfModelDelegate<TModel>(TModel model);
    public delegate TModel CreateNewModelDelegate<TModel>();

    public interface IEvolutionSettings<TModel> : INeuralNetworkSettings
    {
        Random RandomnessProvider { get; }

        int AmountOfWorstGenomesToRemovePerGeneration { get; }
        int AmountOfGenomesInPopulation { get; }

        CalculateFitnessOfModelDelegate<TModel> FitnessCalculationMethod { get; }
        CreateNewModelDelegate<TModel> ModelFactoryMethod { get; }
    }
}
