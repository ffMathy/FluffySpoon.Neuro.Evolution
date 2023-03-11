using System;
using FluffySpoon.Neuro.Evolution.Domain.Network;

namespace FluffySpoon.Neuro.Evolution.Domain.Genomics;

public interface IGenome<TSimulation> : IDisposable where TSimulation : ISimulation
{
    TSimulation Simulation { get; }
    INeuralNetwork NeuralNetwork { get; }

    void Mutate();

    void Tick();

    IGenome<TSimulation> Clone();
}