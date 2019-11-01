using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGeneration<TSimulation> where TSimulation : ISimulation
    {
        IReadOnlyCollection<IGenome<TSimulation>> Genomes { get; }

        Task<IGeneration<TSimulation>> EvolveAsync();

        Task<IGenome<TSimulation>> CrossTwoRandomGenomesAsync();
        
        IGenome<TSimulation> PickRandomGenome();

        IGeneration<TSimulation> Clone();

        Task TickAsync();

        void RemoveWorstPerformingGenomes();

        void AddGenome(IGenome<TSimulation> genome);
        void RemoveGenome(IGenome<TSimulation> genome);
    }
}
