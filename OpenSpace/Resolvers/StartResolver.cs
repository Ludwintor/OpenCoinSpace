using OpenSpace.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OpenSpace.Resolvers
{
    internal sealed class StartResolver : ICommandResolver
    {
        private static readonly string _startMessage = $"""
            *Open Space*
            
            Интерфейс для взаимодействия с проектами Open Project
            На данный момент доступно:
            \- Стейкинг
            """;
        private static readonly InlineKeyboardMarkup _startMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"{Emojis.PURSE}Кошелёк", Callbacks.WALLET)],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.GEM}Стейкинг", Callbacks.STAKING)],
            [
                InlineKeyboardButton.WithCallbackData($"{Emojis.GEAR}Настройки", Callbacks.SETTINGS),
                InlineKeyboardButton.WithCallbackData($"Информация{Emojis.BOOK}", Callbacks.INFO)
            ]
        ]);

        public Task ResolveCommandAsync(TelegramBot bot, Message message, CancellationToken ct)
        {
            if (message.Chat.Type != ChatType.Private)
                return Task.CompletedTask;
            return bot.Client.SendTextMessageAsync(message.Chat, _startMessage, parseMode: ParseMode.MarkdownV2,
                                                   replyMarkup: _startMarkup, disableWebPagePreview: true, cancellationToken: ct);
        }
    }
}
