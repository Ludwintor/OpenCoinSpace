using OpenSpace.Entities;
using OpenSpace.Toncenter.Entities;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal interface ITonService
    {
        ValueTask<JettonWallet?> GetJettonWalletAsync(string owner, string masterAddress);

        ValueTask<NftItem[]> GetNftItemsAsync(string collection, string owner);

        TonConnect GetUserConnector(long userId);
    }
}
