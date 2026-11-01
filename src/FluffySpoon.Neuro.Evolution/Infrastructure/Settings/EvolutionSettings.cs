using System;
using FluffySpoon.Neuro.Evolution.Domain;

namespace FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

public class EvolutionSettings<TSimulation> : IEvolutionSettings<TSimulation> where TSimulation : ISimulation
{
    public int AmountOfWorstGenomesToRemovePerGeneration { get; set; }
    public int AmountOfGenomesInPopulation { get; set; }

    public CreateNewModelDelegate<TSimulation> SimulationFactoryMethod { get; set; }
    public TickCallbackDelegate<TSimulation> PostTickMethod { get; set; }

    public Random RandomnessProvider { get; set; } = new();

    /// <summary>
    /// The chance (0 to 1) that each individual weight or bias is changed when a genome is mutated.
    /// </summary>
    public float MutationProbability { get; set; } = 0.1f;

    /// <summary>
    /// The standard deviation of the gaussian noise added to a weight or bias when it is mutated.
    /// </summary>
    public float MutationStrength { get; set; } = 0.5f;
    
    public int[] NeuronCounts { get; set; }

}