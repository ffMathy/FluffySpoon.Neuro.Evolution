using System;
using FluffySpoon.Neuro.Evolution.Domain;

namespace FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

public class EvolutionSettings<TSimulation> : IEvolutionSettings<TSimulation> where TSimulation : ISimulation
{
    public int AmountOfWorstGenomesToRemovePerGeneration { get; set; }
    public int AmountOfGenomesInPopulation { get; set; }

    public CreateNewModelDelegate<TSimulation> SimulationFactoryMethod { get; set; }
    public TickCallbackDelegate<TSimulation> PostTickMethod { get; set; }

    public Random RandomnessProvider { get; set; }
    
    public float MutationProbability { get; set; }
    public float MutationStrength { get; set; }
    
    public int[] NeuronCounts { get; set; }

}