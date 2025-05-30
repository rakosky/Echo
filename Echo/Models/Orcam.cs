using Echo.Services;
using static Echo.Extern.User32;

namespace Echo.Models
{
    public class Orcam
    {
        public string Name { get; set; } = string.Empty;
        public List<OrcamCommand> Commands { get; set; } = new List<OrcamCommand>();

    }


    public class OrcamCommand
    {
        public KeyPressType Type { get; set; }
        public ScanCodeShort Key { get; set; }
        public int Delay { get; set; }
    }
}