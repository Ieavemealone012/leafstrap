using System.Drawing;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Bloxstrap.Integrations
{
    public sealed class WindowManipulation
    {
        private readonly int _robloxProcessId;

        public WindowManipulation(int robloxProcessId)
        {
            _robloxProcessId = robloxProcessId;
        }

        public void Start()
        {
            if (!App.Settings.Prop.FakeBorderlessFullscreen)
                return;

            const string LOG_IDENT = "WindowManipulation::BorderlessFullscreen";

            try
            {
                IntPtr windowHandle = WaitForMainWindow();
                if (windowHandle == IntPtr.Zero)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Roblox did not expose a main window in time; skipping borderless mode.");
                    return;
                }

                HWND hwnd = (HWND)windowHandle;
                const int GWL_STYLE = -16;
                const int WS_CAPTION = 0x00C00000;
                const int WS_THICKFRAME = 0x00040000;
                const int WS_MINIMIZEBOX = 0x00020000;
                const int WS_MAXIMIZEBOX = 0x00010000;
                const int WS_SYSMENU = 0x00080000;

                int style = PInvoke.GetWindowLong(hwnd, (WINDOW_LONG_PTR_INDEX)GWL_STYLE);
                style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);

                Rectangle bounds = Screen.FromHandle(windowHandle).Bounds;

                PInvoke.SetWindowLong(hwnd, (WINDOW_LONG_PTR_INDEX)GWL_STYLE, style);
                PInvoke.SetWindowPos(
                    hwnd,
                    (HWND)IntPtr.Zero,
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height + 1,
                    SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);

                App.Logger.WriteLine(LOG_IDENT, $"Applied borderless mode at {bounds.Width}x{bounds.Height}.");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Unable to apply borderless fullscreen.");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        private IntPtr WaitForMainWindow()
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                using var process = Process.GetProcessById(_robloxProcessId);
                process.Refresh();

                if (process.HasExited)
                    return IntPtr.Zero;

                if (process.MainWindowHandle != IntPtr.Zero)
                    return process.MainWindowHandle;

                Thread.Sleep(100);
            }

            return IntPtr.Zero;
        }
    }
}
