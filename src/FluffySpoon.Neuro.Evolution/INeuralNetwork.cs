﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution
{
    public interface INeuralNetwork
    {
        double[] Ask(double[] input);
        void WipeAllTraining();

        INeuron[] GetAllNeurons();

        Task TrainAsync(IEnumerable<double[]> input, IEnumerable<double[]> expectedOutput);
    }
}
