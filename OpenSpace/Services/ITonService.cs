using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal interface ITonService
    {
        TonConnect GetUserConnector(long userId);
    }
}
