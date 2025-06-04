namespace Echo.Extern
{
    public class KeyboardHook(nint hookID, LowLevelInputProc proc)
    {
        public IntPtr hookID = hookID;
        public LowLevelInputProc _proc = proc;
    }
}
