using System.Collections.Generic;

namespace FluffySpoon.Neuro.Evolution.Domain.Genomics;

public interface IGeneration<TSimulation> where TSimulation : ISimulation
{
    IReadOnlyCollection<IGenome<TSimulation>> Genomes { get; }

    IGeneration<TSimulation> Evolve();
    IGeneration<TSimulation> EvolveInParallel(int maximumDegreeOfParallelism = -1);

    IGeneration<TSimulation> Clone();
    void Simulate();

    void AddGenome(IGenome<TSimulation> genome);
    void RemoveGenome(IGenome<TSimulation> genome);
}