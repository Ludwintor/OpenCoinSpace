using Telegram.Bot.Types;

namespace OpenSpace.Core
{
    public interface IInlineResolver
    {
        Task ResolveInlineAsync(TelegramBot bot, InlineQuery query, CancellationToken ct);
    }
}
