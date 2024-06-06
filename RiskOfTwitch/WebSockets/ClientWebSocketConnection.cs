using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RiskOfTwitch.WebSockets
{
    internal abstract class ClientWebSocketConnection : IDisposable
    {
        ClientWebSocket _client;

        public Uri ConnectionUrl { get; set; }

        public bool AutoReconnect { get; set; } = true;

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public WebSocketState? State => _client?.State;

        readonly CancellationTokenSource _objectDisposedTokenSource = new CancellationTokenSource();

        bool _isReconnecting;

        bool _hasEverConnected;

        bool _isDisposed;

        public ClientWebSocketConnection(Uri url)
        {
            ConnectionUrl = url;
        }

        ~ClientWebSocketConnection()
        {
            Dispose();
        }

        public void Dispose()
        {
            dispose();
            GC.SuppressFinalize(this);
        }

        protected virtual void dispose()
        {
            if (_isDisposed)
                return;

            if (_client != null)
            {
                using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                try
                {
                    Disconnect(timeoutTokenSource.Token).Wait();
                }
                catch
                {
                    _client?.Dispose();
                }

                _client = null;
            }

            _objectDisposedTokenSource.Cancel();
            _objectDisposedTokenSource.Dispose();

            _isDisposed = true;
        }

        void throwIfDisposed()
        {
            lock (this)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException($"{nameof(ClientWebSocketConnection)} ({ConnectionUrl})");
                }
            }
        }

        protected virtual ClientWebSocket createClient()
        {
            ClientWebSocket client = new ClientWebSocket();
            ClientWebSocketOptions options = client.Options;
            options.KeepAliveInterval = ConnectionTimeout;

            return client;
        }

        public async Task Connect(CancellationToken cancellationToken = default)
        {
            throwIfDisposed();

            if (_client != null)
            {
                if (State == WebSocketState.Connecting || State == WebSocketState.Open)
                {
                    Log.Error("Already connected");
                    return;
                }

                Log.Warning("Connecting while there is still an existing client instance, disposing old instance");
                _client.Dispose();
            }

            CancellationTokenSource cancelledOrDisposedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_objectDisposedTokenSource.Token, cancellationToken);

            _client = createClient();
            await _client.ConnectAsync(ConnectionUrl, cancelledOrDisposedTokenSource.Token).ConfigureAwait(false);

            if (!_hasEverConnected)
            {
                _ = Task.Run(() => updateLoop(_objectDisposedTokenSource.Token));
                _hasEverConnected = true;
            }
        }

        public virtual async Task Disconnect(CancellationToken cancellationToken = default)
        {
            if (_client == null)
                return;

            if (State >= WebSocketState.CloseSent)
            {
                _client.Dispose();
                _client = null;
                return;
            }

            CancellationTokenSource cancelledOrDisposedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_objectDisposedTokenSource.Token, cancellationToken);

            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancelledOrDisposedTokenSource.Token).ConfigureAwait(false);
            _client?.Dispose();
            _client = null;
        }

        public async Task Reconnect(TimeSpan delay = default, CancellationToken cancellationToken = default)
        {
            _isReconnecting = true;

            try
            {
                await Disconnect(cancellationToken).ConfigureAwait(false);

                await Task.Delay(delay).ConfigureAwait(false);

                await Connect(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        async Task updateLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_client == null)
                {
                    if (AutoReconnect && !_isReconnecting)
                        await Connect(cancellationToken).ConfigureAwait(false);

                    continue;
                }

                if (State != WebSocketState.Open)
                    continue;

                try
                {
                    await handleNextMessageAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Log.Error_NoCallerPrefix($"Unhandled exception handling web socket message: {e}");

                    // Completely arbitrary delay
                    await Reconnect(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async Task<WebSocketMessage> receiveMessageAsync(CancellationToken cancellationToken)
        {
            const int BUFFER_SIZE = 1024;

            byte[] receivedData = new byte[BUFFER_SIZE];
            int totalReceivedDataLength = 0;
            WebSocketReceiveResult receiveResult;

            do
            {
                while (totalReceivedDataLength >= receivedData.Length)
                {
                    Array.Resize(ref receivedData, receivedData.Length + BUFFER_SIZE);
                }

                ArraySegment<byte> buffer = new ArraySegment<byte>(receivedData, totalReceivedDataLength, receivedData.Length - totalReceivedDataLength);

                receiveResult = await _client.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                totalReceivedDataLength += receiveResult.Count;
            } while (!receiveResult.EndOfMessage);

            return new WebSocketMessage(new ArraySegment<byte>(receivedData, 0, totalReceivedDataLength), receiveResult);
        }

        async Task handleNextMessageAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(ConnectionTimeout);
            using CancellationTokenSource cancelledOrTimedOutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

            WebSocketMessage message;
            try
            {
                message = await receiveMessageAsync(cancelledOrTimedOutTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (timeoutTokenSource.Token.IsCancellationRequested)
                {
                    Log.Info("WebSocket connection timed out, attempting reconnect...");
                    await Reconnect(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

                    return;
                }

                throw;
            }

            if (message.MessageType == WebSocketMessageType.Close)
            {
                if (message.CloseStatus != WebSocketCloseStatus.NormalClosure)
                {
                    if (shouldReconnect(message))
                    {
                        Log.Info($"WebSocket connection closed (status={message.CloseStatus}, description='{message.CloseStatusDescription}'), attempting reconnect...");

                        await Reconnect(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        Log.Info($"WebSocket connection closed (status={message.CloseStatus}, description='{message.CloseStatusDescription}'), not attempting reconnect");

                        AutoReconnect = false;
                        await Disconnect(cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    Log.Info("WebSocket connection closed");
                }

                return;
            }

            await handleSocketMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task handleSocketMessageAsync(WebSocketMessage message, CancellationToken cancellationToken);

        protected virtual bool shouldReconnect(WebSocketMessage closingMessage)
        {
            return true;
        }
    }
}
