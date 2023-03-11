namespace FluffySpoon.Neuro.Evolution.Domain.Genomics;

public interface IGenomeFactory<TSimulation> where TSimulation : ISimulation
{
    IGenome<TSimulation> Create();
}