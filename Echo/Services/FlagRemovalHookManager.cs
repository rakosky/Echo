using Echo.Extern;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Echo.Services
{
    public class FlagRemovalHookManager
    {
        public readonly TimeSpan HookLifetime = TimeSpan.FromSeconds(20);
        
        private KeyboardHook _kbHook;
        private MouseHook _mouseHook;
        private readonly ILogger<FlagRemovalHookManager> _logger;

        private int _inKbCallback = 0;
        private int _inMCallback = 0;


        public FlagRemovalHookManager(ILogger<FlagRemovalHookManager> logger)
        {
            _logger = logger;
        }

        private async Task RefreshHooks()
        {
            if (_kbHook != null)
            {
                while (_inKbCallback > 0)
                {
                    await Task.Delay(10); // Wait for current callback to finish
                }
                InputHookManager.UnhookWindowsHookEx(_kbHook.hookID);
            }
            _kbHook = InputHookManager.CreateKeyboardHook(KeyboardHookCallback);

            if (_mouseHook != null)
            {
                while (_inMCallback > 0)
                {
                    await Task.Delay(10); // Wait for current callback to finish
                }
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
            Interlocked.Increment(ref _inKbCallback);
            try
            {
                if (nCode < 0)
                    return InputHookManager.CallNextHookEx(_mouseHook.hookID, nCode, wParam, lParam);
                var original = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool wasInjected = (original.flags & InputHookManager.LLKHF_INJECTED) != 0;

                //Console.WriteLine($"KB hook Id:{_kbHook.hookID} Key:{original.scanCode} Injected: {wasInjected}");

                if (!wasInjected)
                    return InputHookManager.CallNextHookEx(_kbHook.hookID, nCode, wParam, lParam);

                var modified = original;
                modified.flags &= ~InputHookManager.LLKHF_INJECTED;

                IntPtr modifiedPtr = Marshal.AllocHGlobal(Marshal.SizeOf<KBDLLHOOKSTRUCT>());
                Marshal.StructureToPtr(modified, modifiedPtr, false);

                //Console.WriteLine($"KB hook Id:{_kbHook.hookID} New injected: {(modified.flags & InputHookManager.LLKHF_INJECTED) != 0}");
                // Forward modified struct 
                IntPtr result = InputHookManager.CallNextHookEx(_kbHook.hookID, nCode, wParam, modifiedPtr);

                Marshal.FreeHGlobal(modifiedPtr); // Clean up
                return result;
            }
            finally
            {
                Interlocked.Decrement(ref _inKbCallback);
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Interlocked.Increment(ref _inMCallback);
            try
            {
                if (nCode < 0)
                    return InputHookManager.CallNextHookEx(_mouseHook.hookID, nCode, wParam, lParam);

                MSLLHOOKSTRUCT original = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                bool wasInjected = (original.flags & InputHookManager.LLMHF_INJECTED) != 0;

                //Console.WriteLine($"Mouse moved to {original.pt.x},{original.pt.y} - Injected: {wasInjected}");

                // Clone and remove the injected flag
                if (!wasInjected)
                    return InputHookManager.CallNextHookEx(_mouseHook.hookID, nCode, wParam, lParam);

                var modified = original;
                modified.flags &= ~InputHookManager.LLMHF_INJECTED;

                IntPtr fakeLParam = Marshal.AllocHGlobal(Marshal.SizeOf<MSLLHOOKSTRUCT>());
                Marshal.StructureToPtr(modified, fakeLParam, false);

                //Console.WriteLine($"new injected: {(modified.flags & InputHookManager.LLMHF_INJECTED) != 0}");

                IntPtr result = InputHookManager.CallNextHookEx(_mouseHook.hookID, nCode, wParam, fakeLParam);

                Marshal.FreeHGlobal(fakeLParam);
                return result;
            }
            finally
            {
                Interlocked.Decrement(ref _inMCallback);
            }
        }

        public async Task StartHookManagementLoop()
        {
            while (true)
            {
                try
                {
                    _logger.LogInformation("Refreshing hooks");
                    await RefreshHooks();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error setting hooks");
                }

                await Task.Delay((int)HookLifetime.TotalMilliseconds);
            }

        }
    }
}
