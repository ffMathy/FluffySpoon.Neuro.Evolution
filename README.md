# FluffySpoon.Neuro.Evolution

Neuroevolution for .NET: a population of small feed-forward neural networks is evolved with a genetic algorithm.
Each generation, every genome drives its own `ISimulation` until it ends; the best genomes survive unchanged and
the worst ones are replaced by mutated offspring of the survivors.

Key conventions:

- **Lower `Fitness` is better.** For "distance travelled", return `-distanceTravelled`.
- Inputs can be any range, but the network learns far better if you normalize them to roughly `[-1, 1]`.
- Outputs are always in the range `[0, 1]`. Use `NeuronInterpretationHelper` to turn them into booleans or enum values.

```csharp
var serviceCollection = new ServiceCollection();
serviceCollection.AddFluffySpoonNeuroEvolution(new EvolutionSettings<CarSimulation>
{
    AmountOfGenomesInPopulation = 50,
    AmountOfWorstGenomesToRemovePerGeneration = 40,

    // 3 input neurons, 6 neurons in a hidden layer, 2 output neurons
    NeuronCounts = new[] { 3, 6, 2 },

    // chance that each weight/bias is changed on mutation, and the standard deviation of the change
    MutationProbability = 0.15f,
    MutationStrength = 0.5f,

    SimulationFactoryMethod = () => new CarSimulation()
});

var serviceProvider = serviceCollection.BuildServiceProvider();

var generation = serviceProvider.GetRequiredService<IGeneration<CarSimulation>>();
for (var i = 0; i < 100; i++)
    generation = generation.Evolve();

class CarSimulation : ISimulation
{
    enum SteeringType { TurnLeft, DoNothing, TurnRight }

    public int DistanceTravelled { get; private set; }

    // lower is better
    public double Fitness => -DistanceTravelled;

    public bool HasEnded => DistanceTravelled > 10;

    public float[] GetInputs()
    {
        // e.g. 3 LIDAR readings, normalized by the maximum sensor range
        return new[] { 100f / 200f, 80f / 200f, 30f / 200f };
    }

    public void Tick(float[] outputs)
    {
        var steering = NeuronInterpretationHelper.InterpretAsEnum<SteeringType>(outputs[0]);
        var accelerate = NeuronInterpretationHelper.InterpretAsBoolean(outputs[1]);

        if (accelerate)
            DistanceTravelled++;
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```
