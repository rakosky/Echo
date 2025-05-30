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
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private LowLevelKeyboardProc _proc;

        private List<OrcamCommand> _macroCommands;

        public OrcamRecorder(
            ILogger<OrcamRecorder> logger)
        {
            _logger = logger;
        }

        public async Task<Orcam> Record()
        {
            _logger.LogInformation("Recording macro...");

             _macroCommands = new List<OrcamCommand>();

            _timeHolder = TimeGetTime();

            _proc = HookCallback;
            _hookID = SetHook(_proc);

            while (_hookID != IntPtr.Zero)
            {
                await Task.Delay(100);
            }
            _logger.LogInformation("Recording stopped.");

            return new Orcam()
            {
                Commands = new(_macroCommands),
            };

        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }
 

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr _hookID = IntPtr.Zero;
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
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                return IntPtr.Zero;
            }
            var now = TimeGetTime();
            long elapsed = now - _timeHolder;
            _timeHolder = now;
            _macroCommands.Add(new OrcamCommand()
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
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }


        public static ScanCodeShort VirtualKeyToScanCode(VirtualKeyShort virtualKeyShort)
        {
            int virtualKey = (int)virtualKeyShort;
            uint scanCode = MapVirtualKey((uint)virtualKey, 0);
            return (ScanCodeShort)scanCode;
        }



        [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        private static extern uint TimeGetTime();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    }
}
