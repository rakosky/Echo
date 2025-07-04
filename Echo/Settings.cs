using Echo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Echo.Extern.User32;

namespace Echo
{
    public class Settings
    {
        public HotkeySettings Hotkeys { get; set; }

        public ScanCodeShort[] InjectableKeys { get; set; }

    }
}
