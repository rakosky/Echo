using static Echo.Extern.User32;

namespace Echo.Services
{
    public class HotkeySettings
    {
        public ScanCodeShort JumpKey { get; set; }
        public ScanCodeShort RopeLiftKey { get; set; }
        public ScanCodeShort NpcChatKey { get; set; }
        public ScanCodeShort MenuKey { get; set; }
        public ScanCodeShort AttackKey { get; set; }
        public ScanCodeShort MapKey { get; set; }
        
    }
}
