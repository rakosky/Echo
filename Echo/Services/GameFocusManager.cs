using Echo.Extern;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Echo.Services
{
    public class GameFocusManager
    {
        readonly ProcessProvider _processProvider;
        readonly ILogger<GameFocusManager> _logger;

        public GameFocusManager(
            ProcessProvider processProvider,
            ILogger<GameFocusManager> logger)
        {
            _processProvider = processProvider;
            _logger = logger;
        }

        public event Action<bool> OnFocusChanged;
        public bool Focused { get; private set; }

        public async Task StartFocusCheckLoop()
        {
            while (true)
            {
                try
                {
                    var process = _processProvider.TryGetProcess();
                    if (process is not null && process.MainWindowHandle != IntPtr.Zero && User32.GetForegroundWindow() == process.MainWindowHandle)
                    {
                        //focused
                        if (!Focused)
                        {
                            Focused = true;
                            _logger.LogInformation("Game window focused.");
                            OnFocusChanged?.Invoke(Focused);
                        }
                    }
                    else
                    {
                        if (Focused)
                        {
                            Focused = false;
                            _logger.LogInformation("Game window lost focus.");
                            OnFocusChanged?.Invoke(Focused);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking game focus: {ex.Message}");
                }
                await Task.Delay(50); // Check every second
            }
        }
        public void SetFocus()
        {
            try
            {
                var process = _processProvider.Process;
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    User32.SetForegroundWindow(process.MainWindowHandle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bringing game window to foreground.");
            }

        }
    }
}
