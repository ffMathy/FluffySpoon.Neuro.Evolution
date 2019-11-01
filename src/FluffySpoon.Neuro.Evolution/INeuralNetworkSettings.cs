namespace FluffySpoon.Neuro.Evolution
{
    public interface INeuralNetworkSettings
    {
        double NeuronMutationProbability { get; }
        int[] NeuronCounts { get; }
    }
}
