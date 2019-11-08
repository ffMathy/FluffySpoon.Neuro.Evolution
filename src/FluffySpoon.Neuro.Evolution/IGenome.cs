using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGenome<TSimulation> : IDisposable where TSimulation : ISimulation
    {
        TSimulation Simulation { get; }
        INeuralNetwork NeuralNetwork { get; }

        void AddBasePair(double[] inputs, double[] outputs);
        void RemoveBasePair(double[] inputs);

        Task<IGenome<TSimulation>> CrossWithAsync(IGenome<TSimulation> other);
        Task EnsureTrainedAsync();

        Task SwapWithAsync(IGenome<TSimulation> other);
        Task MutateAsync();

        Task<double[]> AskAsync(double[] input);
        Task TickAsync();

        Task<IGenome<TSimulation>> CloneAsync();
    }
}
