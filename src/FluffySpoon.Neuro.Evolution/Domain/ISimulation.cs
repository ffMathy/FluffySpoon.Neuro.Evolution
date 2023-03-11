namespace FluffySpoon.Neuro.Evolution.Domain;

public interface ISimulation
{
    double Fitness { get; }
    bool HasEnded { get; }
    
    float[] GetInputs();
    void Tick(float[] outputs);
}