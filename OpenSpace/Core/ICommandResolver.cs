using OpenSpace.Bot;
using Telegram.Bot.Types;

namespace OpenSpace.Core
{
    public interface ICommandResolver
    {
        Task ResolveCommandAsync(TelegramBot bot, Message message, CancellationToken ct);
    }
}
