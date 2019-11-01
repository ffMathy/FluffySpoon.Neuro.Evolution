using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGeneration
    {
        IGenome[] Genomes { get; }

        Task<IGeneration> EvolveAsync(
            int? genomesToRemove);

        Task<IGenome> CrossTwoRandomGenomesAsync();
        
        IGenome PickRandomGenome();

        void RemoveWorstPerformingGenomes(int? amount);

        void AddGenome(IGenome genome);
        void RemoveGenome(IGenome genome);
    }
}
