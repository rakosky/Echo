using Echo.Extern;
using Echo.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Echo.Extern.User32;

namespace Echo.Services
{
    public class OrcamRecorder
    {
        ILogger<OrcamRecorder> _logger;

        private KeyboardHook _hook;

        private List<OrcamCommand> _commands;

        public OrcamRecorder(
            ILogger<OrcamRecorder> logger)
        {
            _logger = logger;
        }

        public async Task<Orcam> Record()
        {
            _logger.LogInformation("Recording...");

             _commands = new List<OrcamCommand>();

            _timeHolder = TimeGetTime();

            _hook = InputHookManager.CreateKeyboardHook(HookCallback);

            while (_hook.hookID != IntPtr.Zero)
            {
                await Task.Delay(100);
            }
            _logger.LogInformation("Recording stopped.");

            return new Orcam()
            {
                Commands = new(_commands),
            };

        }

        private uint _timeHolder;
        private IntPtr HookCallback(
                int nCode, IntPtr wParam, IntPtr lParam)
        {
            string keyboardEvent = "";
            switch (wParam)
            {
                case 256: case 260: keyboardEvent = "DOWN"; break;
                case 257: case 261: keyboardEvent = "UP"; break;
            }

            int vkCode = Marshal.ReadInt32(lParam);
            var scanCode = VirtualKeyToScanCode((VirtualKeyShort)vkCode);

            // Exit on F12
            if (scanCode == ScanCodeShort.F12)
            {
                InputHookManager.UnhookWindowsHookEx(_hook.hookID);
                _hook.hookID = IntPtr.Zero;
                return IntPtr.Zero;
            }
            var now = TimeGetTime();
            long elapsed = now - _timeHolder;
            _timeHolder = now;
            _commands.Add(new OrcamCommand()
            {
                Type = wParam switch
                {
                    256 or 260 => KeyPressType.DOWN,
                    257 or 261 => KeyPressType.UP,
                    _ => KeyPressType.PRESS
                },
                Key = scanCode,
                Delay = (int)elapsed
            });

            _logger.LogInformation($"Key: {scanCode}, Event: {keyboardEvent}, Delay: {elapsed}");
            
            return InputHookManager.CallNextHookEx(_hook.hookID, nCode, wParam, lParam);
        }


        public static ScanCodeShort VirtualKeyToScanCode(VirtualKeyShort virtualKeyShort)
        {
            int virtualKey = (int)virtualKeyShort;
            uint scanCode = MapVirtualKey((uint)virtualKey, 0);
            return (ScanCodeShort)scanCode;
        }

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        private static extern uint TimeGetTime();
    }
}
