using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpenSpace.Logging
{
    internal static class LogEvents
    {
        public static EventId Bot { get; } = new(100, "TelegramBot");
        public static EventId TonConnect { get; } = new(101, "TonConnect");
    }
}
