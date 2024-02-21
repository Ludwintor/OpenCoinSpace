using System.Text;
using Microsoft.Extensions.Logging;
using OpenSpace.Core;
using OpenSpace.Entities;
using OpenSpace.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OpenSpace.Resolvers
{
    internal sealed class StakingResolver : ICallbackResolver
    {
        private const ulong HUNDRED_PERCENT = 10000000000;
        private const string STAKING_STATUS = $$"""
            {{Emojis.GEM}} APY: {0}%
            {{Emojis.GIFT}} Пул наград: {1} OPEN
            """;

        private static readonly InlineKeyboardMarkup _stakingMenuMarkup = new([
            [
                InlineKeyboardButton.WithCallbackData($"{Emojis.INBOX} Положить", Callbacks.STAKE),
                InlineKeyboardButton.WithCallbackData($"{Emojis.OUTBOX} Забрать", Callbacks.UNSTAKE),
            ],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.GIFT} Пожертвовать в пул", Callbacks.DONATE)],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.RECYCLE} Обновить", Callbacks.STAKING)],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.TRIGRAM} Назад", Callbacks.MAIN)]
        ]);

        private readonly ITonService _ton;
        private readonly ILogger _logger;
        private readonly string _stakingAddress;
        private readonly string _tokenAddress;
        private readonly int _tokenDecimals;

        public StakingResolver(ITonService ton, ILogger logger, Config config)
        {
            _ton = ton;
            _logger = logger;
            _stakingAddress = config.StakingAddress;
            _tokenAddress = config.TokenAddress;
            _tokenDecimals = config.TokenDecimals;
        }

        public Task ResolveCallbackAsync(TelegramBot bot, CallbackQuery query, string id, string? arg, CancellationToken ct)
        {
            return id switch
            {
                Callbacks.STAKING => HandleStakingAsync(bot, query, ct),
                Callbacks.STAKE => HandleStakeAsync(bot, query, ct),
                Callbacks.UNSTAKE => HandleUnstakeAsync(bot, query, ct),
                _ => Task.CompletedTask
            };
        }

        private async Task HandleStakingAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {
            StakingInfo stakingInfo = await _ton.GetStakingInfoAsync(_stakingAddress).ConfigureAwait(false);
            StringBuilder sb = new();
            AppendStakingInfo(sb, stakingInfo);
            await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, sb.ToString(),
                                                  replyMarkup: _stakingMenuMarkup, cancellationToken: ct);
        }

        private async Task HandleStakeAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {

        }

        private async Task HandleUnstakeAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {

        }

        private StringBuilder AppendStakingInfo(StringBuilder sb, StakingInfo stakingInfo)
        {
            double rewardSupply = (double)stakingInfo.RewardSupply / Math.Pow(10, _tokenDecimals);
            double baseReward = (double)stakingInfo.BaseRewardSupply / Math.Pow(10, _tokenDecimals);
            double maxPercent = (double)stakingInfo.MaxPercent / HUNDRED_PERCENT;
            double apy = maxPercent * rewardSupply / baseReward;
            string text = string.Format(STAKING_STATUS, (apy * 100d).ToString("0.00"), rewardSupply.ToString("0.00"));
            sb.AppendLine(text);
            return sb;
        }
    }
}
