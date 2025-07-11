using Echo.Services;
using System.Text.Json.Serialization;
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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public KeyPressType Type { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScanCodeShort Key { get; set; }
        public int Delay { get; set; }
    }
}