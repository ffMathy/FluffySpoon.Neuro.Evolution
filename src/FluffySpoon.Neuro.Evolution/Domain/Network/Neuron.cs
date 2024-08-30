using System.Collections.Generic;
using System.Linq;

namespace FluffySpoon.Neuro.Evolution.Domain.Network;

public class Neuron(Layer layer)
{
    public float Bias { get; set; }

    public Layer Layer { get; } = layer;

    public List<Dendrite> DendritesTowardsNextLayer { get; } = new();
}

public class Dendrite(Neuron source, Neuron destination)
{
    public float Weight { get; set; }

    public Neuron Source { get; } = source;
    public Neuron Destination { get;  } = destination;
}