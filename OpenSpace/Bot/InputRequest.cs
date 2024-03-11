using Telegram.Bot.Types;

namespace OpenSpace.Bot
{
    internal sealed class InputRequest : IDisposable
    {
        private readonly TaskCompletionSource<Message?> _tcs;
        private readonly CancellationTokenSource _cts;

        public InputRequest(TimeSpan timeout)
        {
            _tcs = new();
            _cts = new(timeout);
            _cts.Token.Register(request => TimeoutRequest((InputRequest)request!), this);
        }

        public Task<Message?> Task => _tcs.Task;

        public void SetMessage(Message? message)
        {
            _tcs.TrySetResult(message);
        }

        public void Dispose()
        {
            if (!_tcs.Task.IsCompleted)
                _tcs.TrySetResult(null);
            _cts.Dispose();
        }

        private static void TimeoutRequest(InputRequest request)
        {
            if (!request._tcs.Task.IsCompleted)
                request._tcs.TrySetResult(null);
        }
    }
}
