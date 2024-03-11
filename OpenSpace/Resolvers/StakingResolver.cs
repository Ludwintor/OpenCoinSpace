using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenSpace.Bot;
using OpenSpace.Core;
using OpenSpace.Entities;
using OpenSpace.Services;
using OpenSpace.Toncenter.Entities;
using OpenSpace.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TonSdk.Connect;
using TonSdk.Core;
using Message = Telegram.Bot.Types.Message;

namespace OpenSpace.Resolvers
{
    internal sealed class StakingResolver : ICallbackResolver
    {
        private const ulong HUNDRED_PERCENT = 10000000000;
        private const string DATE_TIME_FORMAT = "yyyy-MM-dd hh:mm:ss";
        private const string STAKING_STATUS = $$"""
            {{Emojis.GEM}} Стейкинг

            {{Emojis.MONEY_BAG}} APY: {0}%
            {{Emojis.GIFT}} Пул наград: {1} OPEN
            """;
        private const string STAKE_TEXT = """
            Внимание! Токены невозможно забрать раньше выбранного периода блокировки.
            Ваши токены хранятся на смарт-контракте и никто не имеет к ним доступа.

            В качестве доказательства владения токенами вам выдаётся NFT которую нельзя передавать.
            По истечении периода блокировки - вам необходимо как можно скорее забрать ваши токены и награду.
            Удержание токенов после окончания периода блокировки не приносит дополнительную награду.

            Введите время блокировки (количество дней от {0} до {1})
            """;
        private const string STAKE_AMOUNT_TEXT = $$"""
            {{Emojis.GEM}} Стейкинг

            Период блокировки: {0} дней

            Баланс: `{1}` OPEN

            Минимальная сумма для блокировки: `{2}` OPEN
            """;
        private const string STAKE_AMOUNT_ALLOW = """
            Введите сумму для блокировки
            """;
        private const string STAKE_AMOUNT_DISALLOW = """
            У вас недостаточно OPEN для блокировки
            """;
        private const string UNSTAKE_NO_NFT = """
            Заблокированные токены не найдены
            """;
        private const string UNSTAKE_SELECT_NFT = """
            Выберите позицию которую можно забрать
            """;
        private const string UNSTAKE_STILL_LOCK = """
            Данная позиция заблокирована до {0} UTC
            """;
        private const string TRANSACTION_CONFIRM = """
            Подтвердите транзакцию в вашем кошельке
            """;
        private const string BALANCE_TEXT = """
            Баланс: `{0}` OPEN
            """;
        private const string ZERO_BALANCE = """
            На вашем кошельке нет токенов
            """;
        private const string DONATE_AMOUNT = """
            Введите сумму для пожертвования в пул наград
            """;
        private const string INVALID_DAY_ERROR = """
            Пожалуйста, введите целое количество дней в заданном диапазоне
            """;
        private const string INVALID_AMOUNT_ERROR = """
            Пожалуйста, введите верную сумму токенов, не превышающую ваш баланс
            """;
        private const string NO_WALLET_TEXT = """
            Кошелёк не подключен
            """;

        private static readonly InlineKeyboardButton[] _backButton =
            [InlineKeyboardButton.WithCallbackData($"{Emojis.TRIGRAM} Назад", Callbacks.STAKING)];

        private static readonly InlineKeyboardMarkup _stakingMenuMarkup = new([
            [
                InlineKeyboardButton.WithCallbackData($"{Emojis.INBOX} Положить", Callbacks.STAKE),
                InlineKeyboardButton.WithCallbackData($"{Emojis.OUTBOX} Забрать", Callbacks.UNSTAKE),
            ],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.GIFT} Пожертвовать в пул", Callbacks.DONATE)],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.RECYCLE} Обновить", Callbacks.STAKING)],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.TRIGRAM} Назад", Callbacks.MAIN)]
        ]);
        private static readonly InlineKeyboardMarkup _noWalletMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"{Emojis.SATELLITE} Подключить кошелёк", Callbacks.CONNECT_WALLET)],
            _backButton
        ]);
        private static readonly InlineKeyboardMarkup _backMarkup = new([
            _backButton
        ]);

        private readonly ITonService _ton;
        private readonly IStakingService _staking;
        private readonly ILogger _logger;
        private readonly string _stakingAddress;
        private readonly string _tokenAddress;
        private readonly double _tokenMultiplier;

        public StakingResolver(ITonService ton, IStakingService staking, ILogger logger, Config config)
        {
            _ton = ton;
            _staking = staking;
            _logger = logger;
            _stakingAddress = config.StakingAddress;
            _tokenAddress = config.TokenAddress;
            _tokenMultiplier = Math.Pow(10, config.TokenDecimals);
        }

        public Task ResolveCallbackAsync(TelegramBot bot, CallbackQuery query, string id, string? arg, CancellationToken ct)
        {
            return id switch
            {
                Callbacks.STAKING => HandleStakingAsync(bot, query, ct),
                Callbacks.STAKE => HandleStakeAsync(bot, query, ct),
                Callbacks.UNSTAKE => HandleUnstakeAsync(bot, query, arg, ct),
                Callbacks.DONATE => HandleDonateAsync(bot, query, ct),
                _ => Task.CompletedTask
            };
        }

        private async Task HandleStakingAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {
            StakingInfo stakingInfo = await _staking.GetStakingInfoAsync(_stakingAddress).ConfigureAwait(false);
            StringBuilder sb = new();
            AppendStakingInfo(sb, stakingInfo);
            string text = sb.ToString();
            if (string.Compare(query.Message!.Text, text, CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols) != 0)
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, text,
                    replyMarkup: _stakingMenuMarkup, cancellationToken: ct).ConfigureAwait(false);
            else
                await bot.Client.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task HandleStakeAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {
            TonConnect connector = _ton.GetUserConnector(query.From.Id);
            if (!connector.IsConnected && !await connector.RestoreConnection().ConfigureAwait(false))
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, NO_WALLET_TEXT,
                    replyMarkup: _noWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }

            StakingInfo stakingInfo = await _staking.GetStakingInfoAsync(_stakingAddress).ConfigureAwait(false);
            StringBuilder sb = new();
            AppendStakingInfo(sb, stakingInfo);
            sb.Append("\n\n");
            TimeSpan max = TimeSpan.FromSeconds(stakingInfo.MaxLockup);
            TimeSpan min = TimeSpan.FromSeconds(stakingInfo.MinLockup);
            int maxDays = max.Days;
            int minDays = min.Days;
            sb.AppendFormat(STAKE_TEXT, minDays.ToString(), maxDays.ToString());
            await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, sb.ToString(),
                replyMarkup: _backMarkup, cancellationToken: ct).ConfigureAwait(false);
            int? days = await bot.WaitInputParsingAsync(query.From.Id, query.Message.Chat.Id,
                input => ParseDays(input, minDays, maxDays), null, ct).ConfigureAwait(false);
            if (days == null)
                return;

            // still check connector, it may have closed connection at this point but do it silently on error
            connector = _ton.GetUserConnector(query.From.Id);
            if (!connector.IsConnected && !await connector.RestoreConnection().ConfigureAwait(false))
                return;
            sb.Clear();
            string address = connector.Account.Address!.ToString();
            JettonWallet? wallet = await _ton.GetJettonWalletAsync(address, _tokenAddress).ConfigureAwait(false);
            double balance = wallet != null ? (double)wallet.Value.Balance / _tokenMultiplier : 0d;
            string balanceStr = balance.ToString("0.###");
            double minAmount = (double)stakingInfo.MinStake / _tokenMultiplier;
            sb.AppendFormat(STAKE_AMOUNT_TEXT, days.Value.ToString(), MessageHelper.EscapeMarkdown(balanceStr),
                MessageHelper.EscapeMarkdown(minAmount.ToString()));
            sb.AppendLine();
            sb.Append(balance >= minAmount ? STAKE_AMOUNT_ALLOW : STAKE_AMOUNT_DISALLOW);
            await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, sb.ToString(),
                parseMode: ParseMode.MarkdownV2, replyMarkup: _backMarkup, cancellationToken: ct).ConfigureAwait(false);
            double? amount = await bot.WaitInputParsingAsync(query.From.Id, query.Message.Chat.Id,
                input => ParseStakeAmount(input, minAmount, balance), null, ct).ConfigureAwait(false);
            if (amount == null)
                return;

            connector = _ton.GetUserConnector(query.From.Id);
            if (!connector.IsConnected && !await connector.RestoreConnection().ConfigureAwait(false))
                return;
            await EditAcceptTransactionMessage(bot, query.Message, connector, ct)
                .ConfigureAwait(false);
            try
            {
                await _staking.SendStakeAsync(connector, new(wallet!.Value.Address), Coins.FromNano(amount * _tokenMultiplier),
                    (ulong)TimeSpan.FromDays(days.Value).TotalSeconds).ConfigureAwait(false);
            }
            catch (UserRejectsError) { }
        }

        private async Task HandleUnstakeAsync(TelegramBot bot, CallbackQuery query, string? arg, CancellationToken ct)
        {
            // TODO: show total staked balance
            TonConnect connector = _ton.GetUserConnector(query.From.Id);
            if (!connector.IsConnected && !await connector.RestoreConnection().ConfigureAwait(false))
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, NO_WALLET_TEXT,
                    replyMarkup: _noWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }
            string address = connector.Account.Address!.ToString();
            NftItem[] items = await _ton.GetNftItemsAsync(_stakingAddress, address).ConfigureAwait(false);
            if (items.Length == 0)
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, UNSTAKE_NO_NFT,
                    replyMarkup: _backMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }
            if (arg == null || !ulong.TryParse(arg, out ulong index))
            {
                await DisplayAvailableNfts(bot, query, items, ct).ConfigureAwait(false);
                return;
            }

            NftItem? selected = null;
            foreach (NftItem item in items)
                if (item.Index == index)
                {
                    selected = item;
                    break;
                }
            if (!selected.HasValue)
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, UNSTAKE_NO_NFT,
                    replyMarkup: _backMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }
            string nftAddress = selected.Value.Address;
            NftStakeInfo? stakeInfo = await _staking.GetNftStakeInfoAsync(nftAddress).ConfigureAwait(false);
            if (!stakeInfo.HasValue)
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, UNSTAKE_NO_NFT,
                    replyMarkup: _backMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }
            DateTimeOffset unlockTime = DateTimeOffset.FromUnixTimeSeconds((long)stakeInfo.Value.UnlockTime);
            if (DateTimeOffset.UtcNow < unlockTime)
            {
                string text = string.Format(UNSTAKE_STILL_LOCK, unlockTime.ToString(DATE_TIME_FORMAT));
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, text,
                    replyMarkup: _backMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }
            await EditAcceptTransactionMessage(bot, query.Message!, connector, ct)
                .ConfigureAwait(false);
            try
            {
                await _staking.SendUnstakeAsync(connector, new(nftAddress)).ConfigureAwait(false);
            }
            catch (UserRejectsError) { }
        }

        private async Task HandleDonateAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {
            TonConnect connector = _ton.GetUserConnector(query.From.Id);
            if (!connector.IsConnected && !await connector.RestoreConnection().ConfigureAwait(false))
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, NO_WALLET_TEXT,
                    replyMarkup: _noWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }
            string address = connector.Account.Address!.ToString();
            JettonWallet? wallet = await _ton.GetJettonWalletAsync(address, _tokenAddress).ConfigureAwait(false);
            double balance = wallet != null ? (double)wallet.Value.Balance / _tokenMultiplier : 0d;
            StringBuilder sb = new(string.Format(BALANCE_TEXT, balance.ToString("0.###")));
            sb.Append("\n\n").Append(DONATE_AMOUNT);
            await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, sb.ToString(),
                parseMode: ParseMode.MarkdownV2, replyMarkup: _backMarkup, cancellationToken: ct).ConfigureAwait(false);
            double? amount = await bot.WaitInputParsingAsync(query.From.Id, query.Message!.Chat.Id,
                input => ParseDonateAmount(input, balance), null, ct).ConfigureAwait(false);
            if (amount == null)
                return;

            connector = _ton.GetUserConnector(query.From.Id);
            if (!connector.IsConnected && !await connector.RestoreConnection().ConfigureAwait(false))
                return;

            await EditAcceptTransactionMessage(bot, query.Message, connector, ct)
                .ConfigureAwait(false);
            try
            {
                await _staking.SendDonateAsync(connector, new(wallet!.Value.Address), Coins.FromNano(amount * _tokenMultiplier));
            }
            catch (UserRejectsError) { }
        }

        private StringBuilder AppendStakingInfo(StringBuilder sb, StakingInfo stakingInfo)
        {
            double rewardSupply = (double)stakingInfo.RewardSupply / _tokenMultiplier;
            double baseReward = (double)stakingInfo.BaseRewardSupply / _tokenMultiplier;
            double maxPercent = (double)stakingInfo.MaxPercent / HUNDRED_PERCENT;
            double apy = rewardSupply < baseReward ? maxPercent * rewardSupply / baseReward : maxPercent;
            sb.AppendFormat(STAKING_STATUS, (apy * 100d).ToString("0.00"), rewardSupply.ToString("0.00"));
            return sb;
        }

        private async Task DisplayAvailableNfts(TelegramBot bot, CallbackQuery query, NftItem[] items, CancellationToken ct)
        {
            // TODO: Add pagination, now it shows only 6 oldest stakes
            const int MAX_ITEMS = 6;
            List<InlineKeyboardButton[]> buttons = new(Math.Min(items.Length, MAX_ITEMS) + 1);
            Array.Sort(items, (x, y) => x.Index > y.Index ? 1 : x.Index < y.Index ? -1 : 0);
            int added = 0;
            foreach (NftItem item in items)
            {
                NftStakeInfo? stakeInfo = await _staking.GetNftStakeInfoAsync(item.Address).ConfigureAwait(false);
                if (!stakeInfo.HasValue)
                    continue;
                DateTimeOffset unlockDate = DateTimeOffset.FromUnixTimeSeconds((long)stakeInfo.Value.UnlockTime);
                double redeem = (double)stakeInfo.Value.Redeem / _tokenMultiplier;
                DateTimeOffset now = DateTimeOffset.UtcNow;
                string statusEmoji;
                string leftTimeText;
                if (now >= unlockDate)
                {
                    statusEmoji = Emojis.CHECK_MARK;
                    leftTimeText = "Готово";
                }
                else
                {
                    statusEmoji = Emojis.LOCK;
                    TimeSpan leftTime = unlockDate - now;
                    leftTimeText = leftTime.Days > 0 ? $"{leftTime.Days} дней"
                        : leftTime.Hours > 0 ? $"{leftTime.Hours} часов"
                        : leftTime.Minutes > 0 ? $"{leftTime.Minutes} минут"
                        : "<1 минуты";
                }
                InlineKeyboardButton button = InlineKeyboardButton.WithCallbackData(
                    $"{statusEmoji} | {leftTimeText} | {redeem:0.00} OPEN",
                    $"{Callbacks.UNSTAKE}:{item.Index}"
                );
                buttons.Add([button]);
                if (++added == MAX_ITEMS)
                    break;
            }
            buttons.Add(_backButton);
            await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, UNSTAKE_SELECT_NFT,
                replyMarkup: new(buttons), cancellationToken: ct).ConfigureAwait(false);
        }

        private static ParserResult<int> ParseDays(string input, int minDays, int maxDays)
        {
            if (!int.TryParse(input, CultureInfo.InvariantCulture, out int result) ||
                result < minDays || result > maxDays)
                return ParserResult<int>.WithError(INVALID_DAY_ERROR);
            return ParserResult<int>.WithValue(result);
        }

        private static ParserResult<double> ParseStakeAmount(string input, double minAmount, double balance)
        {
            if (!double.TryParse(input.Replace(',', '.'), CultureInfo.InvariantCulture, out double result) ||
                result < minAmount || result > balance)
                return ParserResult<double>.WithError(INVALID_AMOUNT_ERROR);
            return ParserResult<double>.WithValue(result);
        }
        private static ParserResult<double> ParseDonateAmount(string input, double balance)
        {
            if (!double.TryParse(input.Replace(',', '.'), CultureInfo.InvariantCulture, out double result) ||
                result <= 0 || result > balance)
                return ParserResult<double>.WithError(INVALID_AMOUNT_ERROR);
            return ParserResult<double>.WithValue(result);
        }

        private Task<Message> EditAcceptTransactionMessage(TelegramBot bot, Message message, TonConnect connector, CancellationToken ct = default)
        {
            WalletConfig walletConfig = connector.GetWallets()
                .Where(x => x.AppName.Equals(connector.Wallet.Device.AppName, StringComparison.InvariantCultureIgnoreCase))
                .Single();
            InlineKeyboardMarkup markup = new([
                [InlineKeyboardButton.WithUrl($"Перейти в {walletConfig.Name}", walletConfig.UniversalUrl)],
                _backButton
            ]);
            return bot.Client.EditMessageTextAsync(message.Chat, message.MessageId, TRANSACTION_CONFIRM,
                replyMarkup: markup, cancellationToken: ct);
        }
    }
}
