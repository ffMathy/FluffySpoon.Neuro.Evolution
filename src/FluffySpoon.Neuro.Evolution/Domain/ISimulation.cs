using System;

namespace FluffySpoon.Neuro.Evolution.Domain;

public interface ISimulation: IDisposable, IAsyncDisposable
{
    double Fitness { get; }
    bool HasEnded { get; }
    
    float[] GetInputs();
    void Tick(float[] outputs);
}