using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGeneration
    {
        IGenome[] Genomes { get; }

        Task<IGeneration> EvolveAsync();

        Task RemoveWorstPerformingGenerations(int amount);
    }
}
