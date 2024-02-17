using Telegram.Bot.Types;

namespace OpenSpace.Core
{
    public interface ICallbackResolver
    {
        Task ResolveCallbackAsync(TelegramBot bot, CallbackQuery query, string id, string? arg, CancellationToken ct);
    }
}
