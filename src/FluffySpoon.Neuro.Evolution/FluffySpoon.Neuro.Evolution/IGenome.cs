using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGenome
    {
        Task AddChainAsync(double[] inputs, double[] outputs);

        Task<IGenome> CrossWithAsync(IGenome other);

        Task MutateAsync();
    }
}
