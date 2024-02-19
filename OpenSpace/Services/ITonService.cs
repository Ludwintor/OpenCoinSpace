using OpenSpace.Toncenter.Entities;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal interface ITonService
    {
        Task<JettonWallet?> GetJettonWalletAsync(string owner);

        TonConnect GetUserConnector(long userId);
    }
}
