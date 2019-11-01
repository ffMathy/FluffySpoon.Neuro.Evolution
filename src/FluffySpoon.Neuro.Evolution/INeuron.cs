using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FluffySpoon.Neuro.Evolution
{
    public interface INeuron
    {
        double Bias { get; set; }
        ImmutableArray<double> Weights { get; set; }
    }
}
