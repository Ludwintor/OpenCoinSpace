namespace OpenSpace.Entities
{
    internal readonly struct StakingInfo
    {
        public required UInt128 RewardSupply { get; init; }
        public required UInt128 Balance { get; init; }
        public required UInt128 BaseRewardSupply { get; init; }
        public required UInt128 MinStake { get; init; }
        public required ulong MaxPercent { get; init; }
        public required ulong MinPercent { get; init; }
        public required ulong MaxLockup { get; init; }
        public required ulong MinLockup { get; init; }
    }
}
