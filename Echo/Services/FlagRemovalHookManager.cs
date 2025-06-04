using Echo.Extern;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Echo.Services
{
    public class FlagRemovalHookManager
    {
        private KeyboardHook _kbHook;
        private MouseHook _mouseHook;
        private readonly ILogger<FlagRemovalHookManager> _logger;

        public FlagRemovalHookManager(ILogger<FlagRemovalHookManager> logger)
        {
            _logger = logger;
        }

        private void RefreshHooks()
        {
            if (_kbHook != null)
            {
                InputHookManager.UnhookWindowsHookEx(_kbHook.hookID);
            }
            _kbHook = InputHookManager.CreateKeyboardHook(KeyboardHookCallback);

            if (_mouseHook != null)
            {
                InputHookManager.UnhookWindowsHookEx(_mouseHook.hookID);
            }
            _mouseHook = InputHookManager.CreateMouseHook(MouseHookCallback);
        }

        /// <summary>
        /// Callback for the keyboard hook that removes the LLKHF_INJECTED flag from keyboard events.
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)InputHookManager.WM_KEYDOWN)
            {
                var original = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool wasInjected = (original.flags & InputHookManager.LLKHF_INJECTED) != 0;

                Console.WriteLine($"KB hook {original.scanCode} - injected: {wasInjected}");

                // Clone and remove LLKHF_INJECTED
                var modified = original;
                modified.flags &= ~InputHookManager.LLKHF_INJECTED;

                IntPtr modifiedPtr = Marshal.AllocHGlobal(Marshal.SizeOf<KBDLLHOOKSTRUCT>());
                Marshal.StructureToPtr(modified, modifiedPtr, false);

                // Forward modified struct 
                IntPtr result = InputHookManager.CallNextHookEx(_kbHook.hookID, nCode, wParam, modifiedPtr);

                Marshal.FreeHGlobal(modifiedPtr); // Clean up
                return result;
            }



            return InputHookManager.CallNextHookEx(_kbHook.hookID, nCode, wParam, lParam);
        }

        
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)InputHookManager.WM_MOUSEMOVE)
            {
                MSLLHOOKSTRUCT original = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                bool wasInjected = (original.flags & InputHookManager.LLMHF_INJECTED) != 0;

                Console.WriteLine($"Mouse moved to {original.pt.x},{original.pt.y} - Injected: {wasInjected}");

                // Clone and remove the injected flag
                var modified = original;
                modified.flags &= ~InputHookManager.LLMHF_INJECTED;

                IntPtr fakeLParam = Marshal.AllocHGlobal(Marshal.SizeOf<MSLLHOOKSTRUCT>());
                Marshal.StructureToPtr(modified, fakeLParam, false);

                IntPtr result = InputHookManager.CallNextHookEx(_mouseHook.hookID, nCode, wParam, fakeLParam);

                Marshal.FreeHGlobal(fakeLParam);
                return result;
            }

            return InputHookManager.CallNextHookEx(_mouseHook.hookID, nCode, wParam, lParam);
        }

        public async Task StartHookManagementLoop()
        {
            while (true)
            {
                try
                {
                    _logger.LogInformation("Refreshing hooks");
                    RefreshHooks();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error setting hooks");
                }

                await Task.Delay(60000);// Refresh every minute
            }

        }
    }
}
