﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface ISimulation
    {
        double Fitness { get; }
        bool HasEnded { get; }

        Task ResetAsync();
        Task<double[]> GetInputsAsync();
        Task TickAsync(double[] outputs);
    }
}
