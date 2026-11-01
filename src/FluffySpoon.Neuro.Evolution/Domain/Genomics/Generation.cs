using System;
using System.Collections.Generic;
using System.Linq;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

namespace FluffySpoon.Neuro.Evolution.Domain.Genomics;

public enum SimulationResult
{
    Ended,
    InProgress
}

/// <summary>
/// A population of genomes. Note that a <b>lower</b> <see cref="ISimulation.Fitness"/> is considered better.
/// </summary>
public class Generation<TSimulation> : IGeneration<TSimulation> where TSimulation : ISimulation
{
    private readonly IEvolutionSettings<TSimulation> _evolutionSettings;
    private readonly IGenomeFactory<TSimulation> _genomeFactory;

    private readonly List<IGenome<TSimulation>> _genomes;

    public IReadOnlyCollection<IGenome<TSimulation>> Genomes => _genomes;

    public Generation(
        IEvolutionSettings<TSimulation> evolutionSettings,
        IGenomeFactory<TSimulation> genomeFactory)
    {
        _genomes = new List<IGenome<TSimulation>>();

        _evolutionSettings = evolutionSettings;
        _genomeFactory = genomeFactory;
    }

    private int AmountOfGenomesToKeep => Math.Clamp(
        _evolutionSettings.AmountOfGenomesInPopulation - _evolutionSettings.AmountOfWorstGenomesToRemovePerGeneration,
        1,
        Math.Max(1, _evolutionSettings.AmountOfGenomesInPopulation));

    public void AddGenome(IGenome<TSimulation> genome)
    {
        _genomes.Add(genome);
    }

    public void RemoveGenome(IGenome<TSimulation> genome)
    {
        if (_genomes.Remove(genome))
            genome.Dispose();
    }

    private SimulationResult Tick()
    {
        InitializeIfNeeded();

        var endedCount = 0;
        foreach (var genome in _genomes)
        {
            genome.Tick();

            if (genome.Simulation.HasEnded)
                endedCount++;
        }

        if (_evolutionSettings.PostTickMethod != null)
        {
            var best = GetGenomesOrderedByFitness()
                .Take(AmountOfGenomesToKeep)
                .ToList();
            _evolutionSettings.PostTickMethod(new Genomes<TSimulation>(_genomes, best));
        }

        return endedCount == _genomes.Count ?
            SimulationResult.Ended :
            SimulationResult.InProgress;
    }

    private IEnumerable<IGenome<TSimulation>> GetGenomesOrderedByFitness() => _genomes
        .OrderBy(x => x.Simulation.Fitness);

    private void InitializeIfNeeded()
    {
        while (_genomes.Count < _evolutionSettings.AmountOfGenomesInPopulation)
            _genomes.Add(_genomeFactory.Create());
    }

    /// <summary>
    /// Runs all simulations of this generation to completion and then breeds the next generation:
    /// the best genomes survive unchanged (elitism), and the worst ones are replaced by mutated
    /// offspring of the survivors. Every genome in the next generation gets a fresh simulation.
    /// </summary>
    public IGeneration<TSimulation> Evolve()
    {
        // ReSharper disable once EmptyEmbeddedStatement
        while (Tick() == SimulationResult.InProgress) ;

        var survivors = GetGenomesOrderedByFitness()
            .Take(AmountOfGenomesToKeep)
            .ToList();

        var nextGeneration = new Generation<TSimulation>(
            _evolutionSettings,
            _genomeFactory);

        foreach (var survivor in survivors)
            nextGeneration.AddGenome(survivor.Clone());

        while (nextGeneration._genomes.Count < _evolutionSettings.AmountOfGenomesInPopulation)
        {
            var child = PickParent(survivors).Clone();
            child.Mutate();
            nextGeneration.AddGenome(child);
        }

        foreach (var genome in _genomes)
            genome.Dispose();

        _genomes.Clear();

        return nextGeneration;
    }

    /// <summary>
    /// Rank-biased selection: better survivors are more likely to become parents,
    /// while still giving the others a chance in order to preserve diversity.
    /// </summary>
    private IGenome<TSimulation> PickParent(IReadOnlyList<IGenome<TSimulation>> survivorsOrderedByFitness)
    {
        var random = _evolutionSettings.RandomnessProvider;
        var r = random.NextDouble();
        var index = (int)(r * r * survivorsOrderedByFitness.Count);
        return survivorsOrderedByFitness[Math.Min(index, survivorsOrderedByFitness.Count - 1)];
    }
}
