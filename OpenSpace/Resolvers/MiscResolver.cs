using OpenSpace.Bot;
using OpenSpace.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OpenSpace.Resolvers
{
    internal sealed class MiscResolver : ICallbackResolver
    {
        private const string SETTINGS_TEXT = $$"""
            {{Emojis.GEAR}} Настройки

            WIP
            """;
        private const string INFO_TEXT = $$"""
            {{Emojis.INFO}} Информация

            Группа проекта [Open Project](https://t.me/OpenCoinCommunity)

            *Награды* \(__не тело__\) стейкинга облагаются 1% комиссии, которые передаются разработчику для поддержки системы и интерфейса

            Разработчик @ludwintor
            """;

        private static readonly InlineKeyboardMarkup _settingsMarkup = new([
            [InlineKeyboardButton.WithCallbackData($"{Emojis.TRIGRAM} Назад", Callbacks.MAIN)]
        ]);        
        private static readonly InlineKeyboardMarkup _infoMarkup = new([
            [InlineKeyboardButton.WithUrl($"{Emojis.BUG} Баг-репорт", "https://t.me/InstantFormsBot/form?startapp=9b7ddccb-86bb-4c47-a79a-708054aef594&startApp=9b7ddccb-86bb-4c47-a79a-708054aef594")],
            [InlineKeyboardButton.WithCallbackData($"{Emojis.TRIGRAM} Назад", Callbacks.MAIN)]
        ]);

        public Task ResolveCallbackAsync(TelegramBot bot, CallbackQuery query, string id, string? arg, CancellationToken ct)
        {
            return id switch
            {
                Callbacks.SETTINGS => HandleSettingsAsync(bot, query, ct),
                Callbacks.INFO => HandleInfoAsync(bot, query, ct),
                _ => Task.CompletedTask
            };
        }

        private static Task<Message> HandleSettingsAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {
            return bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, SETTINGS_TEXT,
                replyMarkup: _settingsMarkup, cancellationToken: ct);
        }

        private static Task<Message> HandleInfoAsync(TelegramBot bot, CallbackQuery query, CancellationToken ct)
        {
            return bot.Client.EditMessageTextAsync(query.Message!.Chat, query.Message.MessageId, INFO_TEXT, 
                parseMode: ParseMode.MarkdownV2, disableWebPagePreview: true, replyMarkup: _settingsMarkup, cancellationToken: ct);
        }
    }
}
