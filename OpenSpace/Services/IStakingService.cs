using OpenSpace.Entities;
using TonSdk.Connect;
using TonSdk.Core;

namespace OpenSpace.Services
{
    internal interface IStakingService
    {
        Task<StakingInfo> GetStakingInfoAsync(string address);

        Task<NftStakeInfo?> GetNftStakeInfoAsync(string address);

        Task<SendTransactionResult?> SendStakeAsync(TonConnect connector, Address senderJettonWallet, Coins amount, ulong lockTime);

        Task<SendTransactionResult?> SendDonateAsync(TonConnect connector, Address senderJettonWallet, Coins amount);

        Task<SendTransactionResult?> SendUnstakeAsync(TonConnect connector, Address nft);
    }
}
