using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FluffySpoon.Neuro.Evolution.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFluffySpoonNeuroEvolution(new NeuralNetworkSettings<Car>() {
                AmountOfGenomesInPopulation = 100,
                AmountOfWorstGenomesToRemovePerGeneration = 10,

                //3 input neurons, 4 neurons in 1st hidden layer, 5 neurons in 2nd hidden layer, 2 output neurons
                NeuronCounts = new [] { 3, 4, 5, 2 },
                
                ModelFactoryMethod = () => new Car(),
                FitnessCalculationMethod = car => -car.DistanceTravelled
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var firstGeneration = serviceProvider.GetRequiredService<IGeneration<Car>>();
            var secondGeneration = await firstGeneration.EvolveAsync();
        }
    }

    class Car
    {
        public int DistanceTravelled { get; private set; }
    }
}
