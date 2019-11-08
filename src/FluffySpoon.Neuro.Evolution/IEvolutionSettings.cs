using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public delegate TSimulation CreateNewModelDelegate<TSimulation>();
    public delegate void TickCallbackDelegate<TSimulation>(IEnumerable<IGenome<TSimulation>> genomes) where TSimulation : ISimulation;

    public interface IEvolutionSettings<TSimulation> : INeuralNetworkSettings where TSimulation : ISimulation
    {
        Random RandomnessProvider { get; }

        int AmountOfWorstGenomesToRemovePerGeneration { get; }
        int AmountOfGenomesInPopulation { get; }

        CreateNewModelDelegate<TSimulation> SimulationFactoryMethod { get; }
        TickCallbackDelegate<TSimulation> PostTickMethod { get; }
    }
}
