using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public interface IGenomeFactory<TSimulation> where TSimulation : ISimulation
    {
        IGenome<TSimulation> Create();
    }
}
