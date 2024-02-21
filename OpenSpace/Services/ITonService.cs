using OpenSpace.Entities;
using OpenSpace.Toncenter.Entities;
using TonSdk.Connect;

namespace OpenSpace.Services
{
    internal interface ITonService
    {
        Task<StakingInfo> GetStakingInfoAsync(string address);

        Task<JettonWallet?> GetJettonWalletAsync(string owner, string masterAddress);

        TonConnect GetUserConnector(long userId);
    }
}
