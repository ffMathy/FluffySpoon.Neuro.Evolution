using System.Collections.Generic;

namespace FluffySpoon.Neuro.Evolution.Domain.Network;

public class Layer
{
    public Layer? Previous { get; set; }
    
    public Layer? Next { get; set; }

    public List<Neuron> Neurons { get; set; } = new();
}