using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGenome
    {
        void AddBasePair(double[] inputs, double[] outputs);
        void RemoveBasePair(double[] inputs);

        Task CrossWithAsync(IGenome other);
        void SwapWith(IGenome other);
        Task MutateAsync();

        Task<INeuron[]> GetTrainedNeuronsAsync();

        Task<double[]> AskAsync(double[] input);
    }
}
