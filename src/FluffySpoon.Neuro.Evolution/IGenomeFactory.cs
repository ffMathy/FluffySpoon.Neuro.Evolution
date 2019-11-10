using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGenomeFactory<TSimulation> where TSimulation : ISimulation
    {
        IGenome<TSimulation> Create();
    }
}
