using System;

namespace FluffySpoon.Neuro.Evolution.Infrastructure.Settings;

public interface INeuralNetworkSettings
{
    Random RandomnessProvider { get; }
    
    float MutationProbability { get; }
    float MutationStrength { get; }
    int[] NeuronCounts { get; }
}