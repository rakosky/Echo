using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using static Echo.Extern.User32;

namespace Echo.Services
{
    public class InputSender
    {
        readonly ProcessProvider _processProvider;

        public InputSender(ProcessProvider processProvider)
        {
            _processProvider = processProvider;
        }

        public void ReleaseAllPressed()
        {
            // Determine currently pressed keys and release them
            for (int i = 0; i < 256; i++)
            {
                if ((GetAsyncKeyState(i) & 0x8000) != 0)
                {
                    keybd_event((byte)i, 0, 0x0002, 0);
                }
            }
        }
        public void SendKey(ScanCodeShort scanCode, KeyPressType type = KeyPressType.PRESS)
        {
            // helper to build a single INPUT in one line
            static INPUT MakeInput(ScanCodeShort sc, KEYEVENTF flags, int time = 0) => new INPUT
            {
                type = 1, // keyboard
                U = new()
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = sc,
                        dwFlags = flags,
                        time = time,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            };

            switch (type)
            {
                case KeyPressType.DOWN:
                    {
                        INPUT down = MakeInput(scanCode, KEYEVENTF.SCANCODE);
                        SendInput(1, new[] { down }, INPUT.Size);
                        break;
                    }

                case KeyPressType.UP:
                    {
                        INPUT up = MakeInput(scanCode, KEYEVENTF.SCANCODE | KEYEVENTF.KEYUP);
                        SendInput(1, new[] { up }, INPUT.Size);
                        break;
                    }

                case KeyPressType.PRESS:
                default:
                    {
                        INPUT[] seq =
                        {
                            MakeInput(scanCode, KEYEVENTF.SCANCODE, 25),
                            MakeInput(scanCode, KEYEVENTF.SCANCODE | KEYEVENTF.KEYUP)
                        };

                        SendInput((uint)seq.Length, seq, INPUT.Size);
                        break;
                    }
            }
        }


        public void ClickOnPoint(Point clientPoint, bool rClick = false)
        {
            /// get screen coordinates
            var gameProc = _processProvider.Process;

            ClientToScreen(gameProc.MainWindowHandle, ref clientPoint);
            /// set cursor on coords, and press mouse
            SetCursorPos(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT();
            inputMouseDown.type = 0; /// input type mouse
            inputMouseDown.U.mi.dwFlags = rClick ? MOUSEEVENTF.RIGHTDOWN : MOUSEEVENTF.LEFTDOWN; /// button down

            var inputMouseUp = new INPUT();
            inputMouseUp.type = 0; /// input type mouse
            inputMouseUp.U.mi.dwFlags = rClick ? MOUSEEVENTF.RIGHTUP : MOUSEEVENTF.LEFTUP; /// button up

            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

    }

    public enum KeyPressType
    {
        PRESS, DOWN, UP
    }
}
