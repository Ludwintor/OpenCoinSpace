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
        public static EventId Update { get; } = new(100, "UpdateLoop");
    }
}
