using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGeneration
    {
        IReadOnlyCollection<IGenome> Genomes { get; }

        Task<IGeneration> EvolveAsync();

        Task<IGenome> CrossTwoRandomGenomesAsync();
        
        IGenome PickRandomGenome();

        IGeneration Clone();

        void RemoveWorstPerformingGenomes();

        void AddGenome(IGenome genome);
        void RemoveGenome(IGenome genome);
    }
}
