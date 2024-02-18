using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TonSdk.Core;

namespace OpenSpace.Utils
{
    internal static class AddressExtensions
    {
        private static readonly AddressStringifyOptions _bounceable = new(true, false, false, 0);
        private static readonly AddressStringifyOptions _bounceableTestnet = new(true, true, false, 0); 
        private static readonly AddressStringifyOptions _nonbounceable = new(false, false, false, 0);
        private static readonly AddressStringifyOptions _nonbounceableTestnet = new(false, true, false, 0);

        public static string ToBounceable(this Address address, bool testnet = false)
        {
            return address.ToString(AddressType.Base64, testnet ? _bounceableTestnet : _bounceable);
        }

        public static string ToNonBounceable(this Address address, bool testnet = false)
        {
            return address.ToString(AddressType.Base64, testnet ? _nonbounceableTestnet : _nonbounceable);
        }
    }
}
