using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Echo.Extern.User32;

namespace Echo.Models.Settings
{
    public class AppSettings
    {
        public HotkeySettings Hotkeys { get; set; }

        public ScanCodeShort[] InjectableKeys { get; set; }

        public DiscordSettings DiscordSettings { get; set; }

    }
}
