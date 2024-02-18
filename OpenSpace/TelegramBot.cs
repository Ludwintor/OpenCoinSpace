using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSpace.Core;
using OpenSpace.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OpenSpace
{
    public sealed class TelegramBot
    {
        private const char QUERY_DELIMETER = ':';

        private readonly TelegramBotClient _client;
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<(long, long), TaskCompletionSource<Message>> _inputWaiter = [];

        public TelegramBot(string token, IServiceProvider provider)
        {
            _client = new(token);
            _provider = provider;
            _logger = provider.GetRequiredService<ILogger>();
        }

        public ITelegramBotClient Client => _client;

        public async Task RunAsync(ReceiverOptions? options = null)
        {
            User user = await _client.GetMeAsync().ConfigureAwait(false);
            _logger.LogInformation(LogEvents.Bot, "Bot started {Username}", user.Username);
            await _client.ReceiveAsync(UpdateHandler, ErrorHandler, options).ConfigureAwait(false);
        }

        public Task<Message> WaitInputAsync(long userId, long chatId)
        {
            TaskCompletionSource<Message> tcs = new();
            _inputWaiter.AddOrUpdate((userId, chatId), tcs, (key, value) => value);
            return tcs.Task;
        }

        private Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            Task.Run(() =>
            {
                try
                {
                    return update.Type switch
                    {
                        UpdateType.Message => HandleMessage(client, update.Message!, ct),
                        UpdateType.CallbackQuery => HandleCallbackQuery(client, update.CallbackQuery!, ct),
                        UpdateType.InlineQuery => HandleInlineQuery(client, update.InlineQuery!, ct),
                        _ => Task.CompletedTask,
                    };
                }
                catch (Exception e)
                {
                    _logger.LogError(LogEvents.Bot, e, "Handling {Type} Update errored", update.Type.ToString());
                    return Task.CompletedTask;
                }
            }, ct);
            return Task.CompletedTask;
        }

        private Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken ct)
        {
            _logger.LogError(LogEvents.Bot, exception, "Error occured during updates fetch");
            return Task.CompletedTask;
        }

        private Task HandleMessage(ITelegramBotClient client, Message message, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(message.Text))
                return Task.CompletedTask;
            ResolveWaiters(message);
            if (message.Text[0] == '/')
                return HandleCommand(client, message, ct);
            return Task.CompletedTask;
        }

        private Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken ct)
        {
            string[] args = message.Text!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0)
                return Task.CompletedTask;
            ICommandResolver? resolver = _provider.GetKeyedService<ICommandResolver>(args[0]);
            if (resolver == null)
                return Task.CompletedTask;
            return resolver.ResolveCommandAsync(this, message, ct);
        }

        private Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery query, CancellationToken ct)
        {
            string? data = query.Data;
            if (string.IsNullOrEmpty(data))
                return Task.CompletedTask;
            _logger.LogDebug(LogEvents.Bot, "User {User} use callback query {Data}", query.From.Username ?? query.From.Id.ToString(), data);
            ReadOnlySpan<char> span = data.AsSpan();
            int delim = span.IndexOf(QUERY_DELIMETER);
            string id = delim >= 0 ? span[..delim].ToString() : data;
            string? arg = delim >= 0 ? span[(delim + 1)..].ToString() : null;
            ICallbackResolver? resolver = _provider.GetKeyedService<ICallbackResolver>(id);
            if (resolver == null)
                return Task.CompletedTask;
            return resolver.ResolveCallbackAsync(this, query, id, arg, ct);
        }

        private Task HandleInlineQuery(ITelegramBotClient client, InlineQuery query, CancellationToken ct)
        {
            IInlineResolver? resolver = _provider.GetService<IInlineResolver>();
            if (resolver == null)
                return Task.CompletedTask;
            return resolver.ResolveInlineAsync(this, query, ct);
        }

        private void ResolveWaiters(Message message)
        {
            if (message.From != null && _inputWaiter.TryRemove((message.From.Id, message.Chat.Id), out TaskCompletionSource<Message>? tcs))
                tcs.SetResult(message);
        }
    }
}
