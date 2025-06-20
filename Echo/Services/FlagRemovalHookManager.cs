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

        private static readonly int KBFlagsOffset =
            Marshal.OffsetOf<KBDLLHOOKSTRUCT>("flags").ToInt32();

        private static readonly int MFlagsOffset =
            Marshal.OffsetOf<MSLLHOOKSTRUCT>("flags").ToInt32();

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
                if (nCode >= 0)
                {
                    // Read the current flags from the struct at lParam + offset
                    int flags = Marshal.ReadInt32(lParam, KBFlagsOffset);

                    if ((flags & InputHookManager.LLKHF_INJECTED) != 0)
                    {
                        Marshal.WriteInt32(lParam, KBFlagsOffset, flags & ~InputHookManager.LLKHF_INJECTED);
                    }

                }
                return InputHookManager.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
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
                if (nCode >= 0)
                {
                    int flags = Marshal.ReadInt32(lParam, MFlagsOffset);

                    if ((flags & InputHookManager.LLMHF_INJECTED) != 0)
                    {
                        Marshal.WriteInt32(lParam, MFlagsOffset, flags & ~InputHookManager.LLMHF_INJECTED);
                    }

                }
                IntPtr result = InputHookManager.CallNextHookEx(_mouseHook.hookID, nCode, wParam, lParam);

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
