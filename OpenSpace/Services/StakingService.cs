using System.Globalization;
using System.Numerics;
using OpenSpace.Core;
using OpenSpace.Entities;
using OpenSpace.Toncenter;
using OpenSpace.Toncenter.Entities;
using TonSdk.Connect;
using TonSdk.Core;
using TonSdk.Core.Boc;

namespace OpenSpace.Services
{
    internal sealed class StakingService : IStakingService
    {
        private const string GET_STAKING_INFO = "get_staking_info";
        private const string GET_NFT_STAKE_INFO = "get_stake_info";
        private const ulong JETTON_TRANSFER_OP = 0xf8a7ea5;
        private const ulong STAKE_OP = 0x18f2907e;
        private const ulong DONATE_OP = 0x6e872bd4;
        private const ulong PROVE_OWNERSHIP_OP = 0x04ded148;

        private static readonly Cell _emptyCell = new CellBuilder().Build();
        private static readonly Cell _donateCell = new CellBuilder().StoreUInt(DONATE_OP, 32).Build();

        private readonly ToncenterClient _client;
        private readonly Address _staking;
        private readonly SimpleCache<string, StakingInfo> _stakingCache;
        private readonly SimpleCache<string, NftStakeInfo> _nftStakeCache;

        public StakingService(ToncenterClient client, Config config)
        {
            _client = client;
            _staking = new(config.StakingAddress);
            _stakingCache = new(TimeSpan.FromSeconds(4), null, TimeSpan.FromHours(1));
            _nftStakeCache = new(TimeSpan.FromSeconds(5), null, TimeSpan.FromMinutes(30));
        }

        public async Task<StakingInfo> GetStakingInfoAsync(string address)
        {
            if (_stakingCache.TryGet(address, out StakingInfo stakingInfo))
                return stakingInfo;
            GetMethodResult result = await _client.RunGetMethodAsync(address, GET_STAKING_INFO).ConfigureAwait(false);
            StackData[] stack = result.Stack;
            // TODO: Stack reader!!!
            StakingInfo info = new()
            {
                RewardSupply = ParseStackNum<UInt128>(stack[0]),
                Balance = ParseStackNum<UInt128>(stack[1]),
                BaseRewardSupply = ParseStackNum<UInt128>(stack[2]),
                MinStake = ParseStackNum<UInt128>(stack[3]),
                MaxPercent = ParseStackNum<ulong>(stack[4]),
                MinPercent = ParseStackNum<ulong>(stack[5]),
                MaxLockup = ParseStackNum<ulong>(stack[6]),
                MinLockup = ParseStackNum<ulong>(stack[7])
            };
            _stakingCache.AddOrUpdate(address, info);
            return info;
        }

        public async Task<NftStakeInfo?> GetNftStakeInfoAsync(string address)
        {
            if (_nftStakeCache.TryGet(address, out NftStakeInfo cached))
                return cached;
            GetMethodResult result = await _client.RunGetMethodAsync(address, GET_NFT_STAKE_INFO).ConfigureAwait(false);
            if (result.ExitCode != 0)
                return null;
            StackData[] stack = result.Stack;
            NftStakeInfo info = new()
            {
                LockTime = ParseStackNum<ulong>(stack[0]),
                UnlockTime = ParseStackNum<ulong>(stack[1]),
                Body = ParseStackNum<UInt128>(stack[2]),
                Redeem = ParseStackNum<UInt128>(stack[3]),
            };
            _nftStakeCache.AddOrUpdate(address, info);
            return info;
        }

        public Task<SendTransactionResult?> SendStakeAsync(TonConnect connector, Address senderJettonWallet, Coins amount, ulong lockTime)
        {
            Cell payload = new CellBuilder()
                .StoreUInt(STAKE_OP, 32)
                .StoreUInt(lockTime, 64)
                .Build();
            // TODO: Generate query id
            return SendJettonTransfer(connector, senderJettonWallet, 123, amount, new(0.08m), payload);
        }

        public Task<SendTransactionResult?> SendDonateAsync(TonConnect connector, Address senderJettonWallet, Coins amount)
        {
            return SendJettonTransfer(connector, senderJettonWallet, 123, amount, new(0.08m), _donateCell);
        }

        public Task<SendTransactionResult?> SendUnstakeAsync(TonConnect connector, Address nft)
        {
            Cell payload = new CellBuilder()
                .StoreUInt(PROVE_OWNERSHIP_OP, 32) // prove ownership op
                .StoreUInt(0, 64) // query id
                .StoreAddress(_staking) // destination
                .StoreRef(_emptyCell) // forward_payload
                .StoreBit(true) // with content
                .Build();
            Message[] messages = [new Message(nft, new(0.16m), null, payload)];
            SendTransactionRequest request = new(messages, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 120);
            return connector.SendTransaction(request);
        }

        private Task<SendTransactionResult?> SendJettonTransfer(TonConnect connector, Address senderWallet, 
            ulong queryId, Coins amount, Coins forwardValue, Cell? forwardPayload)
        {
            CellBuilder cb = new CellBuilder()
                .StoreUInt(JETTON_TRANSFER_OP, 32) // op
                .StoreUInt(queryId, 64) // query id
                .StoreCoins(amount) // amount
                .StoreAddress(_staking) // destination
                .StoreAddress(connector.Wallet.Account.Address) // response
                .StoreBit(false) // no custom payload
                .StoreCoins(forwardValue); // forward value
            if (forwardPayload != null)
                cb.StoreBit(true).StoreRef(forwardPayload); // forward payload
            else
                cb.StoreBit(false); // no forward payload
            Message[] messages = [new Message(senderWallet, new(0.16m), null, cb.Build())];
            SendTransactionRequest request = new(messages, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120);
            return connector.SendTransaction(request);
        }

        private static T ParseStackNum<T>(StackData data) where T : INumber<T>
        {
            return T.Parse(StripHexPrefix(data.Value), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private static ReadOnlySpan<char> StripHexPrefix(ReadOnlySpan<char> prefixedHex)
        {
            if (prefixedHex.StartsWith("0x"))
                return prefixedHex[2..];
            return prefixedHex;
        }
    }
}
