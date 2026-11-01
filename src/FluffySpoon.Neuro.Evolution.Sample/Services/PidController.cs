using System;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    /// <summary>
    /// A discrete PID controller that is updated once per step, so no time delta is involved.
    /// The integral stops growing while the output is limited, so it doesn't overshoot when coming back from a limit.
    /// </summary>
    public class PidController
    {
        private readonly double _proportionalGain;
        private readonly double _integralGain;
        private readonly double _derivativeGain;
        private readonly double _minimumOutput;
        private readonly double _maximumOutput;

        private double _integral;
        private double? _previousError;

        public PidController(
            double proportionalGain,
            double integralGain,
            double derivativeGain,
            double minimumOutput,
            double maximumOutput)
        {
            _proportionalGain = proportionalGain;
            _integralGain = integralGain;
            _derivativeGain = derivativeGain;
            _minimumOutput = minimumOutput;
            _maximumOutput = maximumOutput;
        }

        public double Update(double error)
        {
            var derivative = _previousError == null ? 0 : error - _previousError.Value;
            _previousError = error;

            var integral = _integral + (_integralGain * error);
            var output = (_proportionalGain * error) + integral + (_derivativeGain * derivative);

            var limitedOutput = Math.Clamp(output, _minimumOutput, _maximumOutput);
            if (limitedOutput == output)
                _integral = integral;

            return limitedOutput;
        }

        /// <summary>
        /// Continues from the given output without a jump, as if the controller had been producing it all along.
        /// </summary>
        public void Reset(double output)
        {
            _integral = output;
            _previousError = null;
        }
    }
}
