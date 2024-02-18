using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private const string DEFAULT_CONFIG = "config.json";

        private static IServiceProvider _serviceProvider = null!;

        private static async Task Main(string[] args)
        {
            Config config = ReadConfig(args);
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(config); // TODO: Probably options pattern?
            services.AddSingleton<ILogger, ConsoleLogger>(provider => new ConsoleLogger(LogLevel.Debug));
            services.AddSingleton<ITonConnectRepository, TonConnectRepository>();
            services.AddHttpClient<ITonService, TonService>(client =>
            {
                client.BaseAddress = new("https://toncenter.com/api/v3/");
                client.DefaultRequestHeaders.Add("X-API-Key", config.ToncenterApi);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            AddResolvers(services);
            _serviceProvider = services.BuildServiceProvider();

            TelegramBot bot = new("6569873991:AAFob2HOh0o9yDOQAijdkD0KEGpTlDBY3kE", _serviceProvider);
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
            collection.AddKeyedSingleton<ICallbackResolver, StartResolver>(Callbacks.MAIN);
            collection.AddKeyedSingleton<ICallbackResolver, WalletResolver>(Callbacks.WALLET);
            collection.AddKeyedSingleton<ICallbackResolver, WalletResolver>(Callbacks.CONNECT_WALLET);
        }

        private static Config ReadConfig(string[] args)
        {
            string path = DEFAULT_CONFIG;
            if (args.Length > 0)
                path = args[0];
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Config>(json) ?? throw new FileNotFoundException("Provide valid config file");
        }
    }

    internal class Config
    {
        [JsonProperty("botApi", Required = Required.Always)]
        public string BotApi { get; private set; } = null!;

        [JsonProperty("toncenterApi", Required = Required.Always)]
        public string ToncenterApi { get; private set; } = null!;

        [JsonProperty("tonConnectManifestUrl")]
        public string TonConnectManifestUrl { get; private set; } = null!;
    }
}
