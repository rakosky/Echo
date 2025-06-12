using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using static Echo.Extern.User32; // Assuming you still use this for mouse control

namespace Echo.Services
{
    public class InputSender : IDisposable
    {
        private readonly IntPtr _context;
        private bool _disposed = false;

        public InputSender()
        {
            EnsureCorrectDllIsLoaded();

            _context = InterceptionNative.interception_create_context();

            InterceptionNative.interception_set_filter(
                _context,
                InterceptionNative.interception_is_keyboard,
                InterceptionFilter.KEYBOARD
            );
        }

        public void SendKey(ScanCodeShort scanCode, KeyPressType type = KeyPressType.PRESS)
        {
            int device = InterceptionNative.interception_wait(_context);

            void Send(InterceptionKeyState state)
            {
                var stroke = new InterceptionKeyStroke
                {
                    code = (ushort)scanCode,
                    state = (ushort)state,
                    information = 0
                };
                InterceptionNative.interception_send(_context, device, ref stroke, 1);
            }

            switch (type)
            {
                case KeyPressType.DOWN:
                    Send(InterceptionKeyState.KEY_DOWN);
                    break;
                case KeyPressType.UP:
                    Send(InterceptionKeyState.KEY_UP);
                    break;
                case KeyPressType.PRESS:
                    Send(InterceptionKeyState.KEY_DOWN);
                    Thread.Sleep(20); // customizable delay
                    Send(InterceptionKeyState.KEY_UP);
                    break;
            }
        }

        public void ReleaseAllPressed()
        {

        }

        public void ClickOnPoint(Point clientPoint, bool rClick = false)
        {
            // Uses user32.dll as before
            SetCursorPos(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT
            {
                type = 0,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = rClick ? MOUSEEVENTF.RIGHTDOWN : MOUSEEVENTF.LEFTDOWN
                    }
                }
            };

            var inputMouseUp = new INPUT
            {
                type = 0,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = rClick ? MOUSEEVENTF.RIGHTUP : MOUSEEVENTF.LEFTUP
                    }
                }
            };

            INPUT[] inputs = { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        private void EnsureCorrectDllIsLoaded()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string arch = Environment.Is64BitProcess ? "x64" : "x86";
            string sourcePath = Path.Combine(baseDir, arch, "interception.dll");
            string destPath = Path.Combine(baseDir, "interception.dll");

            if (!File.Exists(destPath))
            {
                File.Copy(sourcePath, destPath);
            }

            // Optionally confirm DLL presence here
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                InterceptionNative.interception_destroy_context(_context);
                _disposed = true;
            }
        }
    }

    //public enum KeyPressType
    //{
    //    PRESS, DOWN, UP
    //}

    public enum InterceptionKeyState : ushort
    {
        KEY_DOWN = 0x00,
        KEY_UP = 0x01,
    }

    public static class InterceptionFilter
    {
        public const ushort KEYBOARD = 0xFF; // All keyboard events
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InterceptionKeyStroke
    {
        public ushort code;
        public ushort state;
        public ushort information;
    }

    public static class InterceptionNative
    {
        private const string DLL = "interception.dll";

        [DllImport(DLL)] public static extern IntPtr interception_create_context();
        [DllImport(DLL)] public static extern void interception_destroy_context(IntPtr context);

        [DllImport(DLL)]
        public static extern void interception_set_filter(
            IntPtr context, InterceptionPredicate predicate, ushort filter);

        [DllImport(DLL)] public static extern int interception_wait(IntPtr context);

        [DllImport(DLL)]
        public static extern int interception_send(
            IntPtr context, int device, ref InterceptionKeyStroke stroke, int nstroke);

        public delegate int InterceptionPredicate(int device);

        [DllImport(DLL)] public static extern int interception_is_keyboard(int device);
    }
}
