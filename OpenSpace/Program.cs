using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSpace.Core;
using OpenSpace.Logging;
using OpenSpace.Repositories;
using OpenSpace.Resolvers;
using OpenSpace.Services;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace OpenSpace
{
    internal static class Program
    {
        private static IServiceProvider _serviceProvider = null!;

        private static async Task Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<ILogger, ConsoleLogger>(provider => new ConsoleLogger(LogLevel.Debug));
            services.AddSingleton<ITonConnectRepository, TonConnectRepository>();
            services.AddHttpClient<ITonService, TonService>(client =>
            {
                client.BaseAddress = new("https://toncenter.com/api/v3/");
                client.DefaultRequestHeaders.Add("X-API-Key", "");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            AddResolvers(services);
            _serviceProvider = services.BuildServiceProvider();

            TelegramBot bot = new("", _serviceProvider);
            ReceiverOptions options = new()
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.InlineQuery],
                ThrowPendingUpdates = true
            };
            await bot.RunAsync(options);
        }

        private static void AddResolvers(IServiceCollection collection)
        {
            // TODO: Do something like DSharpPlus does - use reflection and attributes
            // to populate resolvers into ServiceCollection to avoid manual adding hell
            // basically use something like command modules to group all commands and callbacks
            collection.AddKeyedSingleton<ICommandResolver, StartResolver>("/start");
            collection.AddKeyedSingleton<ICallbackResolver, WalletResolver>(Callbacks.WALLET);
        }
    }
}
