using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public delegate TSimulation CreateNewModelDelegate<TSimulation>();

    public interface IEvolutionSettings<TSimulation> : INeuralNetworkSettings
    {
        Random RandomnessProvider { get; }

        int AmountOfWorstGenomesToRemovePerGeneration { get; }
        int AmountOfGenomesInPopulation { get; }

        CreateNewModelDelegate<TSimulation> SimulationFactoryMethod { get; }
    }
}
