using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FluffySpoon.Neuro.Evolution.Domain.Genomics;
using FluffySpoon.Neuro.Evolution.Infrastructure.Settings;
using FluffySpoon.Neuro.Evolution.Sample.Helpers;
using FluffySpoon.Neuro.Evolution.Sample.Models;
using FluffySpoon.Neuro.Evolution.Sample.Services;
using Microsoft.Extensions.DependencyInjection;
using Line = Avalonia.Controls.Shapes.Line;
using Point = FluffySpoon.Neuro.Evolution.Sample.Models.Point;

namespace FluffySpoon.Neuro.Evolution.Sample.Views;

public partial class MainWindow : Window
{
    private readonly Random _random;
    private readonly DirectionHelper _directionHelper;

    private bool _keepRunning;
    private bool _render;
    private int _mapElementCount;

    private Map? _map;
    private IGeneration<CarSimulation> _currentGeneration;

    public MainWindow()
    {
        _random = new Random();
        _directionHelper = new DirectionHelper(_random);

        _render = true;

        InitializeComponent();
        GenerateNewMap();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddFluffySpoonNeuroEvolution(
            new EvolutionSettings<CarSimulation>()
            {
                AmountOfGenomesInPopulation = 50,
                AmountOfWorstGenomesToRemovePerGeneration = 40,
                NeuronCounts = new[] { 7, 6, 2 },
                MutationProbability = 0.15f,
                MutationStrength = 0.5f,
                RandomnessProvider = _random,
                SimulationFactoryMethod = () => new CarSimulation(_map ?? throw new Exception("No map has been generated yet.")),
                PostTickMethod = RenderGenomes
            });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _currentGeneration = serviceProvider.GetRequiredService<IGeneration<CarSimulation>>();
    }

    /// <summary>
    /// Called on the training thread after every tick. Blocking until the UI thread has drawn the tick
    /// means the simulations can't change while they are being read, and that ticks can't pile up
    /// faster than they can be drawn. The low priority lets input and rendering go first.
    /// </summary>
    private void RenderGenomes(Genomes<CarSimulation> genomes)
    {
        if (!_render)
            return;

        Dispatcher.UIThread
            .InvokeAsync(() =>
            {
                ClearCars();

                foreach (var genome in genomes.All)
                    RenderCarSimulation(genome.Simulation, false);

                foreach (var genome in genomes.Best)
                    RenderCarSimulation(genome.Simulation, true);
            }, DispatcherPriority.Background)
            .GetAwaiter()
            .GetResult();
    }

    private void GenerateNewMapButton_Click(object sender, RoutedEventArgs e)
    {
        GenerateNewMap();
    }

    private async void TrainGenerationButton_Click(object sender, RoutedEventArgs e)
    {
        await TrainAsync(1);
    }

    private async void TrainMultipleGenerationsButton_Click(object sender, RoutedEventArgs e)
    {
        await TrainAsync(10);
    }

    /// <summary>
    /// Trains on a background thread so that the UI stays responsive, drawing only the last generation of each batch.
    /// The buttons are disabled meanwhile, since only one training run can evolve the current generation,
    /// and since a new map would otherwise be picked up halfway through creating the next generation.
    /// </summary>
    private async Task TrainAsync(int generationsPerBatch)
    {
        SetButtonsEnabled(false);

        try
        {
            do
            {
                await Task.Run(() =>
                {
                    for (var i = 0; i < generationsPerBatch; i++)
                    {
                        _render = i == generationsPerBatch - 1;
                        _currentGeneration = _currentGeneration.Evolve();
                    }
                });
            } while (_keepRunning);
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private void SetButtonsEnabled(bool isEnabled)
    {
        TrainGenerationButton.IsEnabled = isEnabled;
        TrainMultipleGenerationsButton.IsEnabled = isEnabled;
        GenerateNewMapButton.IsEnabled = isEnabled;
    }

    private void GenerateNewMap()
    {
        var mapGeneratorService = new MapGeneratorService(
            _random,
            _directionHelper);
        _map = mapGeneratorService.PickRandomPredefinedMap();

        MapCanvas.Children.Clear();
        RenderMap();
        _mapElementCount = MapCanvas.Children.Count;
    }

    private void ClearCars()
    {
        MapCanvas.Children.RemoveRange(_mapElementCount, MapCanvas.Children.Count - _mapElementCount);
    }

    private void RenderMap()
    {
        if (_map == null)
        {
            throw new InvalidOperationException("No map has been generated yet.");
        }
        
        foreach (var node in _map.Nodes)
        {
            RenderMapNode(node);
        }
    }

    private void Render(IControl element)
    {
        MapCanvas.Children.Add(element);
    }

    private void RenderCarSimulation(CarSimulation carSimulation, bool isBest)
    {
        var car = carSimulation.Car;

        var color = Brushes.Green;
        if (carSimulation.HasEnded)
        {
            color = Brushes.Red;
        }

        if (isBest)
        {
            color = Brushes.Blue;
        }

        var ellipse = new Ellipse()
        {
            Width = car.BoundingBox.Size.Width,
            Height = car.BoundingBox.Size.Height,
            Fill = Brushes.Transparent,
            Stroke = color,
            StrokeThickness = 3,
            Opacity = 1
        };
        Render(ellipse);

        Canvas.SetLeft(ellipse, car.BoundingBox.Location.X);
        Canvas.SetTop(ellipse, car.BoundingBox.Location.Y);

        if (carSimulation.HasEnded)
            return;

        var line = new Line()
        {
            StartPoint = new Avalonia.Point(
                car.BoundingBox.Center.X,
                car.BoundingBox.Center.Y),
            EndPoint = new Avalonia.Point(
                car.BoundingBox.Center.X + (car.ForwardDirectionLine.End.X * (double)car.BoundingBox.Size.Width),
                car.BoundingBox.Center.Y + (car.ForwardDirectionLine.End.Y * (double)car.BoundingBox.Size.Height)),
            Stroke = Brushes.Blue,
            StrokeDashOffset = 2,
            StrokeThickness = 2
        };
        Render(line);

        RenderCarSimulationSensorReadings(carSimulation);
    }

    private void RenderCarSimulationSensorReadings(CarSimulation carSimulation)
    {
        if (carSimulation.HasEnded)
            return;

        var sensorReadings = carSimulation.SensorReadings;
        var sensorReadingsArray = new[]
        {
            sensorReadings.LeftSensor,
            sensorReadings.LeftCenterSensor,
            sensorReadings.CenterSensor,
            sensorReadings.RightCenterSensor,
            sensorReadings.RightSensor
        };

        var car = carSimulation.Car;
        foreach (var sensorReading in sensorReadingsArray)
        {
            var sensorLine = new Line()
            {
                StartPoint = new Avalonia.Point(
                    car.BoundingBox.Center.X,
                    car.BoundingBox.Center.Y),
                EndPoint = new Avalonia.Point(
                    sensorReading.IntersectionPoint.X,
                    sensorReading.IntersectionPoint.Y),
                Stroke = Brushes.Blue,
                Opacity = 0.2,
                StrokeThickness = 1
            };

            Render(sensorLine);
        }
    }

    private void RenderMapNode(MapNode node)
    {
        var rectangle = new Rectangle()
        {
            Width = node.BoundingBox.Size.Width,
            Height = node.BoundingBox.Size.Height,
            Fill = Brushes.White,
            Opacity = 1
        };
        MapCanvas.Children.Add(rectangle);

        Canvas.SetLeft(rectangle, node.BoundingBox.Location.X);
        Canvas.SetTop(rectangle, node.BoundingBox.Location.Y);

        foreach (var progressLine in node.ProgressLines)
            RenderLine(progressLine.Line, Brushes.LightGray, 0.25, progressLine.Offset.ToString());

        foreach (var wallLine in node.WallLines)
            RenderLine(wallLine.Line, Brushes.DimGray, 1, null);
    }

    private void RenderLine(Models.Line line, IBrush brush, double opacity, string? annotation)
    {
        MapCanvas.Children.Add(new Line()
        {
            StartPoint = new Avalonia.Point(
                line.Start.X,
                line.Start.Y),
            EndPoint = new Avalonia.Point(
                line.End.X,
                line.End.Y),
            Opacity = opacity,
            Stroke = brush,
            StrokeThickness = 2
        });

        if (annotation != null)
        {
            RenderTextBlock(line.Center, opacity, annotation);
        }
    }

    private void RenderTextBlock(Point point, double opacity, string annotation)
    {
        var label = new TextBlock()
        {
            Text = annotation,
            FontSize = 10,
            Opacity = opacity,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        Canvas.SetLeft(label, point.X);
        Canvas.SetTop(label, point.Y);

        MapCanvas.Children.Add(label);
    }

    private void KeepRunningCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        _keepRunning = true;
    }

    private void KeepRunningCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        _keepRunning = false;
    }
}