using FluffySpoon.Neuro.Evolution.Sample.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.Neuro.Evolution.Tests;

[TestClass]
public class CarTest
{
    private static Car CreateCarAtSpeed(float deltaVelocity)
    {
        var car = new Car();

        for (var i = 0; i < 200; i++)
            car.Accelerate(deltaVelocity);

        return car;
    }

    private static void TurnAsMuchAsPossible(Car car)
    {
        for (var i = 0; i < 50; i++)
            car.Turn(1);
    }

    [TestMethod]
    public void Turn_ReachesFullLimitAtMinimumSpeed()
    {
        var car = CreateCarAtSpeed(-1);
        Assert.AreEqual(Car.MinimumSpeed, car.SpeedVelocity);

        TurnAsMuchAsPossible(car);

        Assert.AreEqual(Car.MaximumTurnAngleVelocity, car.TurnAngleVelocity, 0.1f);
    }

    [TestMethod]
    public void Turn_IsLimitedToAQuarterAtMaximumSpeed()
    {
        var car = CreateCarAtSpeed(1);
        Assert.AreEqual(Car.MaximumSpeed, car.SpeedVelocity);

        TurnAsMuchAsPossible(car);

        Assert.AreEqual(Car.MaximumTurnAngleVelocity / 4, car.TurnAngleVelocity, 0.1f);
    }

    [TestMethod]
    public void Turn_IsLimitedInBothDirections()
    {
        var car = CreateCarAtSpeed(1);

        for (var i = 0; i < 50; i++)
            car.Turn(-1);

        Assert.AreEqual(-Car.MaximumTurnAngleVelocity / 4, car.TurnAngleVelocity, 0.1f);
    }

    [TestMethod]
    public void Turn_TightensUpWhenSpeedingUpWhileTurning()
    {
        var car = CreateCarAtSpeed(-1);
        TurnAsMuchAsPossible(car);

        for (var i = 0; i < 200; i++)
        {
            car.Accelerate(1);
            car.Turn(0);
        }

        Assert.AreEqual(Car.MaximumTurnAngleVelocity / 4, car.TurnAngleVelocity, 0.1f);
    }
}
