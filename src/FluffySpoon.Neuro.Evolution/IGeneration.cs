using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGeneration<TModel> where TModel : IModel<TModel>
    {
        IReadOnlyCollection<IGenome<TModel>> Genomes { get; }

        Task<IGeneration<TModel>> EvolveAsync();

        Task<IGenome<TModel>> CrossTwoRandomGenomesAsync();
        
        IGenome<TModel> PickRandomGenome();

        IGeneration<TModel> Clone();

        void RemoveWorstPerformingGenomes();

        void AddGenome(IGenome<TModel> genome);
        void RemoveGenome(IGenome<TModel> genome);
    }
}
