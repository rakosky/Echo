namespace Echo.Extern
{
    public class MouseHook(nint hookID, LowLevelInputProc proc)
    {
        public IntPtr hookID = hookID;
        public LowLevelInputProc _proc = proc;
    }
}
