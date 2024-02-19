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
        public static EventId Toncenter { get; } = new(102, "Toncenter");
        public static EventId RestRecv { get; } = new(200, "RestReceive");
        public static EventId RestError { get; } = new(201, "RestError");
        public static EventId RateLimit { get; } = new(202, "RateLimit");
    }
}
