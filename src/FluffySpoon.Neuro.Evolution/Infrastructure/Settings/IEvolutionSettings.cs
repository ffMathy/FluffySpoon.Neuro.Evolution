using System.Collections.Generic;
using FluffySpoon.Neuro.Evolution.Domain;
using FluffySpoon.Neuro.Evolution.Domain.Genomics;

namespace FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

public delegate TSimulation CreateNewModelDelegate<TSimulation>();
public delegate void TickCallbackDelegate<TSimulation>(Genomes<TSimulation> genomes) where TSimulation : ISimulation;

public struct Genomes<TSimulation> where TSimulation : ISimulation
{
    public ICollection<IGenome<TSimulation>> All
    {
        get;
    }

    public ICollection<IGenome<TSimulation>> Best
    {
        get;
    }

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
    int AmountOfWorstGenomesToRemovePerGeneration
    {
        get;
    }
    int AmountOfGenomesInPopulation
    {
        get;
    }

    CreateNewModelDelegate<TSimulation> SimulationFactoryMethod
    {
        get;
    }
    TickCallbackDelegate<TSimulation> PostTickMethod
    {
        get;
    }
}