using System.Diagnostics;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    /// <summary>
    /// Averages the frames drawn since the last time the rate was read. Not thread safe.
    /// </summary>
    public class FpsCounter
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        private int _frames;

        public void AddFrame()
        {
            _frames++;
        }

        public double ReadFramesPerSecond()
        {
            var framesPerSecond = _frames / _stopwatch.Elapsed.TotalSeconds;

            _frames = 0;
            _stopwatch.Restart();

            return framesPerSecond;
        }
    }
}
