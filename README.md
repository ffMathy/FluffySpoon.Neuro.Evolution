# FluffySpoon.Neuro.Evolution

Teach small neural networks to solve a problem without any training data. You describe the problem and how to score an attempt, and the package evolves networks that score well.

It suits problems where you can tell how well something went, but not what the right answer was at each step, such as driving a car, playing a game or tuning a controller.

## How it works

1. A **generation** holds a population of **genomes**. Each genome is a neural network with its own **simulation**.
2. Every network plays its simulation until the simulation ends.
3. The genomes with the best fitness survive unchanged. The rest are replaced by mutated copies of the survivors.
4. The next generation starts over with fresh simulations.

You write the simulation, and the package does the rest.

## Install

```bash
dotnet add package FluffySpoon.Neuro.Evolution
dotnet add package Microsoft.Extensions.DependencyInjection
```

## Quick start

This complete console program evolves a network that solves XOR. Paste it into `Program.cs` and run it.

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.Neuro.Evolution;
using FluffySpoon.Neuro.Evolution.Domain;
using FluffySpoon.Neuro.Evolution.Domain.Genomics;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;

// 1. Configure the evolution.
var services = new ServiceCollection();
services.AddFluffySpoonNeuroEvolution(new EvolutionSettings<XorSimulation>
{
    AmountOfGenomesInPopulation = 50,
    AmountOfWorstGenomesToRemovePerGeneration = 40,
    NeuronCounts = new[] { 2, 4, 1 }, // 2 inputs, a hidden layer of 4 neurons, 1 output
    MutationProbability = 0.2f,
    SimulationFactoryMethod = () => new XorSimulation()
});

// 2. Evolve. Each call returns the next generation.
var generation = services
    .BuildServiceProvider()
    .GetRequiredService<IGeneration<XorSimulation>>();

for (var i = 0; i < 300; i++)
    generation = generation.Evolve();

// 3. Use the result. The first genome is the best one of the previous generation.
var network = generation.Genomes.First().NeuralNetwork;
foreach (var (a, b) in new[] { (0f, 0f), (0f, 1f), (1f, 0f), (1f, 1f) })
    Console.WriteLine($"{a} XOR {b} = {network.Ask(new[] { a, b })[0]:0.00}");

// The problem to solve. A new instance is created for every genome in every generation.
class XorSimulation : ISimulation
{
    private static readonly (float A, float B, float Expected)[] Cases =
    {
        (0, 0, 0), (0, 1, 1), (1, 0, 1), (1, 1, 0)
    };

    private int _index;

    // Lower is better, and the error sum is 0 for a perfect network.
    public double Fitness { get; private set; }

    public bool HasEnded => _index == Cases.Length;

    // What the network gets to see on this tick.
    public float[] GetInputs() => new[] { Cases[_index].A, Cases[_index].B };

    // What the network answered. Move the simulation forward and update the score.
    public void Tick(float[] outputs)
    {
        Fitness += Math.Pow(outputs[0] - Cases[_index].Expected, 2);
        _index++;
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Writing a simulation

Each tick, the network receives `GetInputs()` and its answer goes to `Tick(outputs)`. This repeats until `HasEnded` is `true`.

| Member | What to do |
| --- | --- |
| `GetInputs()` | Return one value per input neuron. Normalize them to roughly `[-1, 1]`, as networks learn far better that way. |
| `Tick(outputs)` | Apply the answer, which has one value per output neuron, each in the range `[0, 1]`. |
| `HasEnded` | Return `true` when the attempt is over. **Make sure this always happens**, for example with a time limit, or the generation never finishes. |
| `Fitness` | **Lower is better.** To reward distance, return `-distance`. |

`NeuronInterpretationHelper` turns outputs into booleans and enums:

```csharp
enum Steering { Left, Straight, Right }

class CarSimulation : ISimulation
{
    public int Distance { get; private set; }
    public int Ticks { get; private set; }

    public double Fitness => -Distance;
    public bool HasEnded => Ticks >= 1000;

    public float[] GetInputs() => new[] { 0.5f, 0.4f, 0.15f }; // e.g. distance sensors divided by their range

    public void Tick(float[] outputs)
    {
        var steering = NeuronInterpretationHelper.InterpretAsEnum<Steering>(outputs[0]);
        var isAccelerating = NeuronInterpretationHelper.InterpretAsBoolean(outputs[1]);

        Ticks++;
        if (isAccelerating)
            Distance++;
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Settings

| Setting | Meaning | Default |
| --- | --- | --- |
| `SimulationFactoryMethod` | Creates a new simulation. | Required |
| `NeuronCounts` | Neurons per layer: inputs, any hidden layers, then outputs. | Required |
| `AmountOfGenomesInPopulation` | How many genomes each generation has. | Required |
| `AmountOfWorstGenomesToRemovePerGeneration` | How many genomes are replaced each generation. At least one always survives. | `0` |
| `MutationProbability` | The chance from 0 to 1 that each weight or bias changes when a genome mutates. | `0.1` |
| `MutationStrength` | The standard deviation of that change. | `0.5` |
| `RandomnessProvider` | The source of randomness. Pass `new Random(42)` for repeatable results. | `new Random()` |
| `PostTickMethod` | Called after every tick of `Evolve` and `Simulate`. See [Watching the simulations](#watching-the-simulations). | None |

## Common tasks

### Checking how the best genome scores

Clone the best genome to get a fresh simulation that you can play yourself:

```csharp
var best = generation.Genomes.First().Clone();
while (!best.Simulation.HasEnded)
    best.Tick();

Console.WriteLine($"Best fitness: {best.Simulation.Fitness}");
```

`Genomes` is empty until the first call to `Evolve` or `Clone`, so do this after evolving at least once.

### Training faster

`EvolveInParallel` runs each simulation on its own thread. It doesn't call `PostTickMethod`, and your simulations must not change any state they share. Results are the same as with `Evolve`.

```csharp
for (var i = 0; i < 300; i++)
    generation = generation.EvolveInParallel();
```

Pass a thread limit, such as `EvolveInParallel(4)`, to leave CPU time for other work.

### Watching the simulations

`PostTickMethod` receives all genomes and the current best ones after every tick, which is useful for drawing or logging:

```csharp
var settings = new EvolutionSettings<XorSimulation>
{
    AmountOfGenomesInPopulation = 50,
    AmountOfWorstGenomesToRemovePerGeneration = 40,
    NeuronCounts = new[] { 2, 4, 1 },
    SimulationFactoryMethod = () => new XorSimulation(),
    PostTickMethod = genomes =>
    {
        var bestFitness = genomes.Best.First().Simulation.Fitness;
        Console.WriteLine($"{genomes.All.Count} genomes, best fitness so far: {bestFitness}");
    }
};
```

### Training while showing a run

Show a copy of the current generation while training continues. `Clone` gives the copy its own networks and simulations, and `Simulate` plays them without breeding a next generation.

```csharp
var shownGeneration = generation.Clone();
var showing = Task.Run(() => shownGeneration.Simulate()); // calls PostTickMethod, for example to draw

while (!showing.IsCompleted)
    generation = generation.EvolveInParallel();

await showing;
```

Call `Clone` before training starts, not during it, and evolve only one generation at a time, because they share `RandomnessProvider`, which isn't thread safe.

## Sample

[`src/FluffySpoon.Neuro.Evolution.Sample`](src/FluffySpoon.Neuro.Evolution.Sample) is a desktop app where cars learn to drive around generated tracks. It uses everything above:

```bash
dotnet run --project src/FluffySpoon.Neuro.Evolution.Sample -c Release
```
