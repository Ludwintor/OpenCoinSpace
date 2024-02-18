using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenSpace.Core;
using OpenSpace.Logging;
using OpenSpace.Services;
using OpenSpace.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TonSdk.Connect;

namespace OpenSpace.Resolvers
{
    internal sealed class WalletResolver : ICallbackResolver
    {
        private const string CONNECTED_WALLET_TEXT = """
            Адрес: {0}
            Баланс: WIP
            """;
        private const string NO_WALLET_TEXT = """
            Кошелёк не подключен
            """;
        private const string SELECT_WALLET_TEXT = """
            Выберите свой кошелёк для подключения
            """;
        private const string ONE_STEP_CONNECT_TEXT = """
            Нажмите на кнопку ниже, чтобы подключить кошелёк (рекомендуется подключать с мобильного устройства)
            """;
        private const string CONNECTION_ERRORED_TEXT = """
            Не удалось подключить кошелёк
            Причина: {0}
            """;

        private static readonly InlineKeyboardMarkup _noWalletMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"{Emojis.SATELLITE}Подключить", Callbacks.CONNECT_WALLET)],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.TRIGRAM}Назад", Callbacks.MAIN)]
        ]);
        private static readonly InlineKeyboardMarkup _connectedWalletMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"{Emojis.SATELLITE}Подключить другой кошелёк", Callbacks.CONNECT_WALLET)],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.TRIGRAM}Назад", Callbacks.MAIN)]
        ]);
        private static readonly InlineKeyboardMarkup _selectWalletMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"Tonkeeper", $"{Callbacks.CONNECT_WALLET}:tonkeeper")],
            [
                InlineKeyboardButton.WithCallbackData($"MyTonWallet", $"{Callbacks.CONNECT_WALLET}:mytonwallet"),
                InlineKeyboardButton.WithCallbackData($"Tonhub", $"{Callbacks.CONNECT_WALLET}:tonhub")
            ]
        ]);

        private readonly ITonService _ton;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<long, Action> _connectedUnsubscribes;

        public WalletResolver(ITonService ton, ILogger logger)
        {
            _ton = ton;
            _logger = logger;
            _connectedUnsubscribes = [];
        }

        public Task ResolveCallbackAsync(TelegramBot bot, CallbackQuery query, string id, string? arg, CancellationToken ct)
        {
            return id switch
            {
                Callbacks.WALLET => HandleWalletAsync(bot, query, ct),
                Callbacks.CONNECT_WALLET => HandleConnectAsync(bot, query, arg, ct),
                _ => Task.CompletedTask
            };
        }

        private async Task HandleWalletAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {
            await bot.Client.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct).ConfigureAwait(false);
            TonConnect connector = _ton.GetUserConnector(query.From.Id);
            if (connector.IsConnected || await connector.RestoreConnection().ConfigureAwait(false))
            {
                string text = string.Format(CONNECTED_WALLET_TEXT, connector.Wallet.Account.Address?.ToNonBounceable());
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, text,
                                                      replyMarkup: _connectedWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
            }
            else
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, NO_WALLET_TEXT,
                                                      replyMarkup: _noWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
            }
        }

        private async Task HandleConnectAsync(TelegramBot bot, CallbackQuery query, string? arg, CancellationToken ct)
        {
            await bot.Client.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(arg))
            {
                await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, SELECT_WALLET_TEXT,
                                                      replyMarkup: _selectWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
                return;
            }
            TonConnect connector = _ton.GetUserConnector(query.From.Id);
            if (_connectedUnsubscribes.TryRemove(query.From.Id, out Action? unsubscribe))
                unsubscribe();
            if (connector.IsConnected)
                await connector.Disconnect().ConfigureAwait(false);
            WalletConfig? config = connector.GetWallets(true, false).Where(x => x.AppName == arg).FirstOrDefault();
            if (!config.HasValue)
            {
                // TODO: something went wrong, there's no such wallet
                // ask user to retry
            }
            // TODO: THIS FUCKING TonSDK IS HORRIBLE. UNSUBSCRIBE ACTION RECEIVED FROM THIS MESSAGE, WHY???
            // write my own C# TonSDK that will be good
            Uri connectUri = new(await connector.Connect(config.Value).ConfigureAwait(false));
            InlineKeyboardMarkup markup = new([
                [InlineKeyboardButton.WithUrl("Подключить!", connectUri.AbsoluteUri)]
            ]);
            unsubscribe = connector.OnStatusChange(wallet => OnWalletConnected(bot, query, wallet, ct), error => OnWalletErrored(bot, query, error, ct));
            _connectedUnsubscribes.TryAdd(query.From.Id, unsubscribe);
            await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, ONE_STEP_CONNECT_TEXT,
                                                  replyMarkup: markup, cancellationToken: ct).ConfigureAwait(false);
        }

        private void OnWalletConnected(TelegramBot bot, CallbackQuery query, Wallet wallet, CancellationToken ct)
        {
            string address = wallet.Account.Address?.ToNonBounceable() ?? string.Empty;
            _logger.LogInformation(LogEvents.Bot, "User {User} connected wallet {Address}", query.From.Username ?? query.From.Id.ToString(), address);
            string text = string.Format(CONNECTED_WALLET_TEXT, address);
            bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, text,
                                            replyMarkup: _connectedWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
        }

        private void OnWalletErrored(TelegramBot bot, CallbackQuery query, string error, CancellationToken ct)
        {
            string text = string.Format(CONNECTION_ERRORED_TEXT, error);
            _logger.LogWarning(LogEvents.Bot, "User {User} cannot connect wallet. Reason: {Error}", query.From.Username ?? query.From.Id.ToString(), error);
            bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, text,
                                                  replyMarkup: _connectedWalletMarkup, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
