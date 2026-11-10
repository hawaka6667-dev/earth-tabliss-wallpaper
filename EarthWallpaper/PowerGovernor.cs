using System;
using System.Drawing;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class PowerGovernor : IDisposable
    {
        private readonly System.Windows.Forms.Timer _pollTimer;
        private readonly Action<bool> _onStateChange;
        private bool _lastPausedState = false;

        public PowerGovernor(Action<bool> onStateChange)
        {
            _onStateChange = onStateChange;

            _pollTimer = new System.Windows.Forms.Timer
            {
                Interval = 800 // Poll every 800ms (near-zero CPU overhead)
            };
            _pollTimer.Tick += OnPollTick;
        }

        public void Start()
        {
            _pollTimer.Start();
        }

        public void Stop()
        {
            _pollTimer.Stop();
        }

        private void OnPollTick(object? sender, EventArgs e)
        {
            IntPtr fg = DesktopWorker.GetForegroundWindow();
            if (fg == IntPtr.Zero)
            {
                UpdateState(false);
                return;
            }

            // If desktop is active (Progman or WorkerW), wallpaper is in view
            if (fg == DesktopWorker.ProgmanHandle || fg == DesktopWorker.WorkerWHandle)
            {
                UpdateState(false);
                return;
            }

            // Check if foreground window is maximized
            if (DesktopWorker.IsZoomed(fg))
            {
                UpdateState(true);
                return;
            }

            // Check if foreground window covers primary screen completely (fullscreen game/app)
            if (DesktopWorker.GetWindowRect(fg, out var rect))
            {
                var screen = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
                if (rect.Left <= screen.Left && rect.Top <= screen.Top &&
                    rect.Right >= screen.Right && rect.Bottom >= screen.Bottom)
                {
                    UpdateState(true);
                    return;
                }
            }

            UpdateState(false);
        }

        private void UpdateState(bool isPaused)
        {
            if (isPaused != _lastPausedState)
            {
                _lastPausedState = isPaused;
                _onStateChange?.Invoke(isPaused);
            }
        }

        public void Dispose()
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
        }
    }
}
