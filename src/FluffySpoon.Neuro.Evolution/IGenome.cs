using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGenome
    {
        INeuralNetwork NeuralNetwork { get; }

        void AddBasePair(double[] inputs, double[] outputs);
        void RemoveBasePair(double[] inputs);

        Task<IGenome> CrossWithAsync(IGenome other);
        Task EnsureTrainedAsync();

        void SwapWith(IGenome other);
        Task MutateAsync();

        Task<double[]> AskAsync(double[] input);
    }
}
