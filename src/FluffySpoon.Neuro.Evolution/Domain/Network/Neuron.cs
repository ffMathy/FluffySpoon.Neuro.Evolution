using System.Collections.Generic;
using System.Linq;

namespace FluffySpoon.Neuro.Evolution.Domain.Network;

public record Neuron(Layer Layer)
{
    public required float Bias { get; set; }

    public List<Dendrite> DendritesTowardsNextLayer { get; } = new();
}

public record Dendrite(Neuron Source, Neuron Destination)
{
    public float Weight { get; set; }
}

public record NeuronComputation(Neuron Neuron)
{
    public float Activation { get; set; } = 0f;
}