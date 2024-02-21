using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenSpace.Core;
using OpenSpace.Logging;
using OpenSpace.Repositories;
using OpenSpace.Resolvers;
using OpenSpace.Services;
using OpenSpace.Toncenter;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using System.Globalization;

namespace OpenSpace
{
    internal static class Program
    {
        private const string DEFAULT_CONFIG = "config.json";

        private static IServiceProvider _serviceProvider = null!;

        private static async Task Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Config config = ReadConfig(args);
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(config); // TODO: Probably options pattern?
            services.AddSingleton<ILogger, ConsoleLogger>(provider => new ConsoleLogger(LogLevel.Trace));
            services.AddSingleton<ITonConnectRepository, TonConnectRepository>();
            services.AddSingleton<ITonService, TonService>();
            services.AddHttpClient<ToncenterClient>(client =>
            {
                client.BaseAddress = new(config.ToncenterUrl);
                client.DefaultRequestHeaders.Add("X-API-Key", config.ToncenterApi);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }).AddPolicyHandler(GetRetryPolicy);
            AddResolvers(services);
            _serviceProvider = services.BuildServiceProvider();

            TelegramBot bot = new(config.BotApi, _serviceProvider);
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
            collection.AddKeyedSingleton<ICallbackResolver, StakingResolver>(Callbacks.STAKING);
            collection.AddKeyedSingleton<ICallbackResolver, StakingResolver>(Callbacks.STAKE);
            collection.AddKeyedSingleton<ICallbackResolver, StakingResolver>(Callbacks.UNSTAKE);
        }

        private static Config ReadConfig(string[] args)
        {
            string path = DEFAULT_CONFIG;
            if (args.Length > 0)
                path = args[0];
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Config>(json) ?? throw new FileNotFoundException("Provide valid config file");
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider provider, HttpRequestMessage request)
        {
            return HttpPolicyExtensions.HandleTransientHttpError()
                                       .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                       .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1),
                                        (output, delay, attempt) =>
                                        {
                                            ILogger logger = provider.GetRequiredService<ILogger>();
                                            if (output.Result != null && output.Result.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                                logger.LogWarning(LogEvents.RateLimit, "Ratelimit hit. Retrying in {Delay} secs. {Attempt}/5", delay.Seconds, attempt);
                                            else
                                                logger.LogError(LogEvents.RestError, "Rest error. Retrying in {Delay} secs. {Attempt}/5", delay.Seconds, attempt);
                                        });
        }
    }

    internal sealed class Config
    {
        [JsonProperty("botApi", Required = Required.Always)]
        public string BotApi { get; private set; } = null!;

        [JsonProperty("toncenterUrl", Required = Required.Always)]
        public string ToncenterUrl { get; private set; } = null!;

        [JsonProperty("toncenterApi", Required = Required.Always)]
        public string ToncenterApi { get; private set; } = null!;

        [JsonProperty("tonConnectManifestUrl", Required = Required.Always)]
        public string TonConnectManifestUrl { get; private set; } = null!;

        [JsonProperty("stakingAddress", Required = Required.Always)]
        public string StakingAddress { get; private set; } = null!;

        [JsonProperty("tokenAddress", Required = Required.Always)]
        public string TokenAddress { get; private set; } = null!;

        [JsonProperty("tokenDecimals", Required = Required.Always)]
        public int TokenDecimals { get; private set; }
    }
}
