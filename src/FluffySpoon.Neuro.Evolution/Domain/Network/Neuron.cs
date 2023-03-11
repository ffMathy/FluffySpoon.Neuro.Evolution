using System.Linq;

namespace FluffySpoon.Neuro.Evolution.Domain.Network;

public class Neuron
{
    public float Bias { get; set; }
    
    public Layer Layer { get; set; }
    
    public Dendrite[] Dendrites { get; set; }
}

public class Dendrite
{
    public float Weight { get; set; }
    
    public Neuron Source { get; set; }
    public Neuron Target { get; set; }
}