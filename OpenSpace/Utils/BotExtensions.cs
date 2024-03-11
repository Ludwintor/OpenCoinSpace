using OpenSpace.Bot;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace OpenSpace.Utils
{
    internal static class BotExtensions
    {
        public static async Task<T?> WaitInputParsingAsync<T>(this TelegramBot bot, long userId, long chatId, Func<string, ParserResult<T>> parser,
                                                              TimeSpan? timeout = null, CancellationToken ct = default) where T : struct
        {
            Message? errorMessage = null;
            ParserResult<T>? parserResult;
            do
            {
                Message? message = await bot.WaitInputAsync(userId, chatId, timeout).ConfigureAwait(false);
                if (message == null)
                {
                    await CleanupErrorMessageAsync(bot, errorMessage, ct).ConfigureAwait(false);
                    return null;
                }
                parserResult = parser(message.Text ?? string.Empty);
                await bot.Client.DeleteMessageAsync(chatId, message.MessageId, ct).ConfigureAwait(false);
                if (!parserResult.Value.IsSuccess)
                {
                    string error = parserResult.Value.Error!;
                    errorMessage = await ShowErrorAsync(bot, chatId, errorMessage, error, ct).ConfigureAwait(false);
                    continue;
                }
            } while (!parserResult.Value.IsSuccess);
            await CleanupErrorMessageAsync(bot, errorMessage, ct).ConfigureAwait(false);
            return parserResult.Value.Value;
        }

        private static Task CleanupErrorMessageAsync(TelegramBot bot, Message? errorMessage, CancellationToken ct)
        {
            if (errorMessage != null)
                return bot.Client.DeleteMessageAsync(errorMessage.Chat, errorMessage.MessageId, ct);
            return Task.CompletedTask;
        }

        private static async Task<Message> ShowErrorAsync(TelegramBot bot, long chatId, Message? errorMessage, string error, CancellationToken ct)
        {
            if (errorMessage == null)
                errorMessage = await bot.Client.SendTextMessageAsync(chatId, error, cancellationToken: ct).ConfigureAwait(false);
            else if (errorMessage.Text != error)
                await bot.Client.EditMessageTextAsync(chatId, errorMessage.MessageId, error, cancellationToken: ct).ConfigureAwait(false);
            return errorMessage;
        }
    }
}
