using OpenSpace.Core;
using OpenSpace.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TonSdk.Connect;

namespace OpenSpace.Resolvers
{
    internal sealed class WalletResolver : ICallbackResolver
    {
        private const string SELECT_WALLET_TEXT = """
            Выберите свой кошелёк для подключения
            """;
        private const string ONE_STEP_CONNECT_TEXT = """
            Нажмите на кнопку ниже, чтобы подключить кошелёк (рекомендуется подключать с мобильного устройства)
            """;

        private static readonly InlineKeyboardMarkup _walletMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"{Emojis.SATELLITE}Подключить", Callbacks.CONNECT_WALLET)]
        ]);
        private static readonly InlineKeyboardMarkup _selectWalletMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"Tonkeeper", $"{Callbacks.CONNECT_WALLET}:tonkeeper")],
            [
                InlineKeyboardButton.WithCallbackData($"MyTonWallet", $"{Callbacks.CONNECT_WALLET}:mytonwallet"),
                InlineKeyboardButton.WithCallbackData($"Tonhub", $"{Callbacks.CONNECT_WALLET}:tonhub")
            ]
        ]);

        private readonly ITonService _ton;

        public WalletResolver(ITonService ton)
        {
            _ton = ton;
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
                // wallet already connected or have saved connection
            }
            else
            {
                // wallet is not connected
                // prompt to connect wallet
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
            WalletConfig? config = connector.GetWallets(true, false).Where(x => x.AppName == arg).FirstOrDefault();
            if (!config.HasValue)
            {
                // something went wrong, there's no such wallet
                // ask user to retry
            }
            string connectLink = await connector.Connect(config.Value).ConfigureAwait(false);
            InlineKeyboardMarkup markup = new([
                [InlineKeyboardButton.WithUrl("Подключить!", connectLink)]
            ]);
            await bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, ONE_STEP_CONNECT_TEXT,
                                                  replyMarkup: markup, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
