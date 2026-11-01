using System;
using System.Runtime.InteropServices;

namespace FluffySpoon.Neuro.Evolution.Sample.Services
{
    /// <summary>
    /// Keeps the screen on and the computer from going to sleep. Windows ties this to the calling thread and
    /// clears it when the process exits, so call both methods from the UI thread. Does nothing on other platforms.
    /// </summary>
    public static class DisplayKeepAwake
    {
        private const uint ContinuousFlag = 0x80000000;
        private const uint SystemRequiredFlag = 0x00000001;
        private const uint DisplayRequiredFlag = 0x00000002;

        public static void Start()
        {
            SetState(ContinuousFlag | SystemRequiredFlag | DisplayRequiredFlag);
        }

        public static void Stop()
        {
            SetState(ContinuousFlag);
        }

        private static void SetState(uint state)
        {
            if (OperatingSystem.IsWindows())
                SetThreadExecutionState(state);
        }

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint state);
    }
}
