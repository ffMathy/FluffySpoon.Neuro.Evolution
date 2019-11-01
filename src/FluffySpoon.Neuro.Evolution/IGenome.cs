using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGenome<TModel>
    {
        double Fitness { get; }

        TModel Model { get; }

        INeuralNetwork NeuralNetwork { get; }

        void AddBasePair(double[] inputs, double[] outputs);
        void RemoveBasePair(double[] inputs);

        Task<IGenome<TModel>> CrossWithAsync(IGenome<TModel> other);
        Task EnsureTrainedAsync();

        Task SwapWithAsync(IGenome<TModel> other);
        Task MutateAsync();

        Task<double[]> AskAsync(double[] input);
    }
}
