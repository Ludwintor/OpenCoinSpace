namespace OpenSpace.Entities
{
    internal struct NftStakeInfo
    {
        public required ulong LockTime { get; init; }
        public required ulong UnlockTime { get; init; }
        public required UInt128 Body { get; init; }
        public required UInt128 Redeem { get; init; }
    }
}
