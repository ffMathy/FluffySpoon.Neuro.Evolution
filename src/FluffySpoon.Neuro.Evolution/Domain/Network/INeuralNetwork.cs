namespace FluffySpoon.Neuro.Evolution.Domain.Network;

public interface INeuralNetwork
{
    INeuralNetwork Clone();

    float[] Ask(float[] input);

    INeuron[] Neurons { get; }
}