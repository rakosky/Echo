using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Echo.Extern
{
    public static class InputHookManager
    {
        public const uint LLKHF_INJECTED = 0x10;
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;

        public const int WH_MOUSE_LL = 14;
        public const int WM_MOUSEMOVE = 0x0200;
        public const uint LLMHF_INJECTED = 0x00000001;

        public static KeyboardHook CreateKeyboardHook(LowLevelInputProc proc)
        {
            if (proc is null)
            {
                throw new ArgumentNullException(nameof(proc));
            }
            IntPtr hookID = SetInputHook(proc, WH_KEYBOARD_LL);

            if (hookID == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            return new KeyboardHook(hookID, proc);
        }

        public static MouseHook CreateMouseHook(LowLevelInputProc proc)
        {
            if (proc is null)
            {
                throw new ArgumentNullException(nameof(proc));
            }
            IntPtr hookID = SetInputHook(proc, WH_MOUSE_LL);
            if (hookID == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            return new MouseHook(hookID, proc);
        }

        private static IntPtr SetInputHook(LowLevelInputProc proc, int hookMethod)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                var hookId = SetWindowsHookEx(hookMethod, proc,
                    GetModuleHandle(curModule.ModuleName), 0);

                Console.WriteLine($"Set hook: {hookMethod}, result: {hookId}");

                return hookId;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelInputProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public delegate IntPtr LowLevelInputProc(
        int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }


}
