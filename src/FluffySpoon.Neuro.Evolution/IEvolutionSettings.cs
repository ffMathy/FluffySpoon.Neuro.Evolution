using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public delegate TSimulation CreateNewModelDelegate<TSimulation>();
    public delegate void TickCallbackDelegate<TSimulation>(Genomes<TSimulation> genomes) where TSimulation : ISimulation;

    public struct Genomes<TSimulation> where TSimulation : ISimulation
    {
        ICollection<IGenome<TSimulation>> All { get; }
        ICollection<IGenome<TSimulation>> Best { get; }

        public Genomes(
            ICollection<IGenome<TSimulation>> all,
            ICollection<IGenome<TSimulation>> best)
        {
            All = all;
            Best = best;
        }
    }

    public interface IEvolutionSettings<TSimulation> : INeuralNetworkSettings where TSimulation : ISimulation
    {
        Random RandomnessProvider { get; }

        int AmountOfWorstGenomesToRemovePerGeneration { get; }
        int AmountOfGenomesInPopulation { get; }

        CreateNewModelDelegate<TSimulation> SimulationFactoryMethod { get; }
        TickCallbackDelegate<TSimulation> PostTickMethod { get; }
    }
}
