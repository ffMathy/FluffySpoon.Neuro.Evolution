using System.Collections.Generic;
using System.Linq;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

namespace FluffySpoon.Neuro.Evolution.Domain.Genomics;

public enum SimulationResult
{
    Ended,
    InProgress
}

public class Generation<TSimulation> : IGeneration<TSimulation> where TSimulation : ISimulation
{
    private readonly IEvolutionSettings<TSimulation> _evolutionSettings;
    private readonly IGenomeFactory<TSimulation> _genomeFactory;

    private HashSet<IGenome<TSimulation>> _genomes;
    private readonly LinkedList<IGenome<TSimulation>> _bestGenomes;

    public IReadOnlyCollection<IGenome<TSimulation>> Genomes => _genomes;

    public Generation(
        IEvolutionSettings<TSimulation> evolutionSettings,
        IGenomeFactory<TSimulation> genomeFactory)
    {
        _genomes = new HashSet<IGenome<TSimulation>>();
        _bestGenomes = new LinkedList<IGenome<TSimulation>>();

        _evolutionSettings = evolutionSettings;
        _genomeFactory = genomeFactory;
    }

    public void AddGenome(IGenome<TSimulation> genome)
    {
        _genomes.Add(genome);
    }

    public void RemoveGenome(IGenome<TSimulation> genome)
    {
        _genomes.Remove(genome);
        genome.Dispose();
    }

    public IGenome<TSimulation> PickRandomGenome()
    {
        var random = _evolutionSettings.RandomnessProvider;
        return _genomes
            .Skip(random.Next(0, _genomes.Count))
            .First();
    }

    /// <returns>True if all simulations have ended.</returns>
    private SimulationResult Tick()
    {
        InitializeIfNeeded();

        var endedCount = 0;

        _bestGenomes.Clear();

        foreach (var genome in _genomes)
        {
            genome.Tick();

            if (genome.Simulation.HasEnded)
                endedCount++;

            CheckIfGenomeIsAmongBestGenomes(genome);
        }

        _evolutionSettings.PostTickMethod?.Invoke(new Genomes<TSimulation>(
            _genomes,
            _bestGenomes));

        return endedCount == _genomes.Count ?
                SimulationResult.Ended :
                SimulationResult.InProgress;
    }

    private void CheckIfGenomeIsAmongBestGenomes(IGenome<TSimulation> genome)
    {
        var lowestBestGenomeSoFar = _bestGenomes.Last?.Value;
        var currentFitness = genome.Simulation.Fitness;
        if (lowestBestGenomeSoFar != null && currentFitness >= lowestBestGenomeSoFar.Simulation.Fitness)
            return;

        var nodeToInsertAt = _bestGenomes.Last;
        while (nodeToInsertAt?.Value != null && nodeToInsertAt.Value.Simulation.Fitness < currentFitness)
            nodeToInsertAt = nodeToInsertAt.Previous;

        if (nodeToInsertAt == null)
        {
            _bestGenomes.AddFirst(genome);
        }
        else
        {
            _bestGenomes.AddBefore(nodeToInsertAt, genome);
        }

        var amountOfGenomesToKeep =
            _evolutionSettings.AmountOfGenomesInPopulation -
            _evolutionSettings.AmountOfWorstGenomesToRemovePerGeneration;
        if (_bestGenomes.Count > amountOfGenomesToKeep)
            _bestGenomes.RemoveLast();
    }

    private void InitializeIfNeeded()
    {
        if (_genomes.Count > 0)
            return;

        while (_genomes.Count < _evolutionSettings.AmountOfGenomesInPopulation)
            _genomes.Add(_genomeFactory.Create());
    }

    public IGeneration<TSimulation> Evolve()
    {
        // ReSharper disable once EmptyEmbeddedStatement
        while(Tick() == SimulationResult.InProgress);

        var generationClone = Clone();
        generationClone.RemoveWorstPerformingGenomes();
        generationClone.BreedNewGenomesBasedOnBestGenome();

        return generationClone;
    }

    private void BreedNewGenomesBasedOnBestGenome()
    {
        var bestGenome = _genomes
            .OrderBy(x => x.Simulation.Fitness)
            .First();
        
        var clone = bestGenome.Clone();
        clone.Mutate();

        AddGenome(clone);
    }

    public void RemoveWorstPerformingGenomes()
    {
        var worstPerformingGenomes = _genomes
            .OrderByDescending(x => x.Simulation.Fitness)
            .Take(_evolutionSettings.AmountOfWorstGenomesToRemovePerGeneration);

        foreach (var genome in worstPerformingGenomes)
            RemoveGenome(genome);
    }

    private Generation<TSimulation> Clone()
    {
        var genomesClone = _genomes.Select(x => x.Clone());
        var generation = new Generation<TSimulation>(
            _evolutionSettings,
            _genomeFactory)
        {
            _genomes = new HashSet<IGenome<TSimulation>>(genomesClone)
        };

        return generation;
    }
}