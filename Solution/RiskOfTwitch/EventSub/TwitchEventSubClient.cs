using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RiskOfTwitch.Chat.Message;
using RiskOfTwitch.Chat.Notification;
using RiskOfTwitch.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiskOfTwitch.EventSub
{
    public sealed class TwitchEventSubClient : ITwitchEventSubMessageHandler
    {
        readonly string _accessToken;
        readonly string _overrideBroadcasterName;

        readonly HashSet<string> _handledMessageIDs = [];

        readonly HashSet<string> _activeSubscriptions = [];

        CancellationTokenSource _disconnectedTokenSource = new CancellationTokenSource();

        TwitchEventSubConnection _mainConnection;
        TwitchEventSubConnection _migratingConnection;

        string _sessionID;

        public string ConnectedToChannel { get; private set; }

        public bool IsConnecting => _mainConnection != null && _mainConnection.State == WebSocketState.Connecting;

        public bool HasConnection => _mainConnection != null && _mainConnection.State == WebSocketState.Open;

        public bool IsFullyConnected => !string.IsNullOrEmpty(_sessionID) && _activeSubscriptions.Count > 0;

        public bool IsMigrating => _migratingConnection != null;

        public event EventHandler<ChannelChatMessageEvent> OnChannelChatMessage;

        public event EventHandler<ChannelChatNotificationEvent> OnChannelChatNotification;

        public event EventHandler<TokenAccessRevokedEventData> OnTokenAccessRevoked;

        public event EventHandler OnFullyConnected;

        public event EventHandler<ConnectionErrorEventArgs> OnConnectionError;

        public TwitchEventSubClient(string accessToken, string overrideBroadcasterName)
        {
            _accessToken = accessToken;
            _overrideBroadcasterName = overrideBroadcasterName;
        }

        public Task Connect(CancellationToken cancellationToken = default)
        {
            return Connect(new Uri("wss://eventsub.wss.twitch.tv/ws"), cancellationToken);
        }

        public async Task Connect(Uri uri, CancellationToken cancellationToken = default)
        {
            _disconnectedTokenSource?.Dispose();
            _disconnectedTokenSource = new CancellationTokenSource();

            using CancellationTokenSource cancelledOrDisconnectedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disconnectedTokenSource.Token);

            if (_mainConnection == null)
            {
                _mainConnection = new TwitchEventSubConnection(uri, this);
                await _mainConnection.Connect(cancelledOrDisconnectedSource.Token).ConfigureAwait(false);
            }
            else
            {
                _mainConnection.ConnectionUrl = uri;
                await _mainConnection.Reconnect(TimeSpan.FromSeconds(0.5), cancelledOrDisconnectedSource.Token).ConfigureAwait(false);
            }
        }

        public async Task Disconnect(CancellationToken cancellationToken = default)
        {
            _disconnectedTokenSource.Cancel();

            if (_mainConnection != null)
            {
                await _mainConnection.Disconnect(cancellationToken);
                _mainConnection?.Dispose();
                _mainConnection = null;
            }

            if (_migratingConnection != null)
            {
                await _migratingConnection.Disconnect(cancellationToken);
                _migratingConnection?.Dispose();
                _migratingConnection = null;
            }

            if (_activeSubscriptions.Count > 0)
            {
                foreach (string subscriptionId in _activeSubscriptions)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                        httpClient.DefaultRequestHeaders.Add("Client-Id", Authentication.CLIENT_ID);

                        _ = httpClient.DeleteAsync($"https://api.twitch.tv/helix/eventsub/subscriptions?id={subscriptionId}", cancellationToken).ContinueWith(task =>
                        {
                            if (task.IsCanceled)
                                return;

                            if (task.IsFaulted)
                            {
                                Log.Error(task.Exception);
                                return;
                            }

                            using HttpResponseMessage subscriptionDeleteResult = task.Result;

                            if (!subscriptionDeleteResult.IsSuccessStatusCode)
                            {
                                Log.Error($"Unable to delete subscription {subscriptionId}: {subscriptionDeleteResult.StatusCode:D} {subscriptionDeleteResult.ReasonPhrase}");
                            }
                            else
                            {
                                Log.Debug($"Removed subscription {subscriptionId}");
                            }
                        });
                    }
                }

                _activeSubscriptions.Clear();
            }

            Log.Debug("Disconnected");
        }

        void beginMigration(string reconnectUrl)
        {
            _migratingConnection = new TwitchEventSubConnection(new Uri(reconnectUrl), this);
            _ = _migratingConnection.Connect();
        }

        public async Task HandleEventAsync(JToken jsonObject, CancellationToken cancellationToken)
        {
            JToken messageIdToken = jsonObject.SelectToken("metadata.message_id", false);
            if (messageIdToken == null)
            {
                Log.Error("Could not deserialize message_id property");
                return;
            }

            if (!_handledMessageIDs.Add(messageIdToken.ToObject<string>()))
                return;

            JToken messageTypeToken = jsonObject.SelectToken("metadata.message_type", false);
            if (messageTypeToken == null)
            {
                Log.Error("Could not deserialize message_type property");
                return;
            }

            using CancellationTokenSource cancelledOrDisconnectedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disconnectedTokenSource.Token);

            string messageType = messageTypeToken.ToObject<string>();
            switch (messageType)
            {
                case "session_welcome":
                    await handleSessionWelcomeMessage(jsonObject, cancelledOrDisconnectedSource.Token);
                    break;
                case "session_keepalive":
                    // This message has no payload, no handling needs to be done. All relevant processing this needs is done above.
                    break;
                case "notification":
                    handleNotificationMessage(jsonObject);
                    break;
                case "session_reconnect":
                    handleSessionReconnectMessage(jsonObject);
                    break;
                case "revocation":
                    handleRevokeMessageAsync(jsonObject);
                    break;
                default:
                    Log.Warning($"Unhandled message type: {messageType}");
                    break;
            }
        }

        void handleSessionReconnectMessage(JToken jsonObject)
        {
            JToken sessionDataToken = jsonObject.SelectToken("payload.session", false);
            if (sessionDataToken == null)
            {
                Log.Error("Could not deserialize session data");
                return;
            }

            WebSocketSessionData sessionData;
            try
            {
                sessionData = sessionDataToken.ToObject<WebSocketSessionData>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize session object: {e}");
                return;
            }

            Log.Debug($"Starting WebSocket migration {_mainConnection.ConnectionUrl}->{sessionData.ReconnectUrl}");

            beginMigration(sessionData.ReconnectUrl);
        }

        void handleRevokeMessageAsync(JToken jsonObject)
        {
            string subscriptionType = null;
            string status = null;

            JToken subscriptionPayloadToken = jsonObject.SelectToken("payload.subscription", false);
            if (subscriptionPayloadToken != null)
            {
                JToken subscriptionTypeToken = subscriptionPayloadToken.SelectToken("type", false);
                if (subscriptionTypeToken != null)
                {
                    subscriptionType = subscriptionTypeToken.ToObject<string>();
                }

                JToken subscriptionStatusToken = subscriptionPayloadToken.SelectToken("status", false);
                if (subscriptionStatusToken != null)
                {
                    status = subscriptionStatusToken.ToObject<string>();
                }
            }

            OnTokenAccessRevoked?.Invoke(this, new TokenAccessRevokedEventData(subscriptionType ?? string.Empty, status ?? string.Empty));
        }

        async Task handleSessionWelcomeMessage(JToken jsonObject, CancellationToken cancellationToken)
        {
            JToken sessionDataToken = jsonObject.SelectToken("payload.session", false);
            if (sessionDataToken == null)
            {
                Log.Error("Could not deserialize session data");
                OnConnectionError?.Invoke(this, new ConnectionErrorEventArgs(ConnectionErrorType.Generic));
                return;
            }

            WebSocketSessionData sessionData;
            try
            {
                sessionData = sessionDataToken.ToObject<WebSocketSessionData>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize session object: {e}");
                OnConnectionError?.Invoke(this, new ConnectionErrorEventArgs(ConnectionErrorType.Generic));
                return;
            }

            if (IsMigrating)
            {
                if (_mainConnection != null)
                {
                    _mainConnection.Dispose();
                    _mainConnection = null;
                }

                _mainConnection = _migratingConnection;

                Log.Debug("Completed WebSocket migration");
                return;
            }

            _sessionID = sessionData.SessionID;

            Result<AuthenticationTokenValidationResponse> tokenValidationResult = await Authentication.GetAccessTokenValidationAsync(_accessToken, cancellationToken).ConfigureAwait(false);
            if (!tokenValidationResult.IsSuccess)
            {
                ConnectionErrorType errorType = ConnectionErrorType.TokenAuthenticationFailed;
                if (tokenValidationResult.Exception is InvalidAccessTokenException)
                {
                    errorType = ConnectionErrorType.TokenInvalid;
                }

                OnConnectionError?.Invoke(this, new ConnectionErrorEventArgs(errorType));
                return;
            }

            AuthenticationTokenValidationResponse tokenValidationResponse = tokenValidationResult.Value;

            async Task<bool> sendSubscription<T>(T message, CancellationToken cancellationToken)
            {
                using HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                client.DefaultRequestHeaders.Add("Client-Id", Authentication.CLIENT_ID);

                StringContent content = new StringContent(JsonConvert.SerializeObject(message));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using HttpResponseMessage subscribeResponseMessage = await client.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content, cancellationToken).ConfigureAwait(false);

                if (!subscribeResponseMessage.IsSuccessStatusCode)
                {
                    Log.Error($"Subscribe failed: {subscribeResponseMessage.StatusCode:D} ({subscribeResponseMessage.StatusCode:G}) {subscribeResponseMessage.ReasonPhrase}");
                    return false;
                }

                using StreamReader responseReader = new StreamReader(await subscribeResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false), Encoding.UTF8);

#if DEBUG
                Log.Debug(await responseReader.ReadToEndAsync().ConfigureAwait(false));
                responseReader.BaseStream.Position = 0;
#endif

                using JsonReader responseJsonReader = new JsonTextReader(responseReader);

                JToken responseObject;
                try
                {
                    responseObject = await JToken.ReadFromAsync(responseJsonReader, cancellationToken).ConfigureAwait(false);
                }
                catch (JsonException e)
                {
                    Log.Error($"Failed to deserialize subscribe response: {e}");
                    return false;
                }

                JArray responseDataArrayToken = responseObject.SelectToken("data", false) as JArray;
                if (responseDataArrayToken == null || responseDataArrayToken.Count <= 0)
                {
                    Log.Error($"Subscribe response contained invalid data");
                    return false;
                }

                JToken subscriptionIdToken = responseDataArrayToken[0].SelectToken("id");
                if (subscriptionIdToken == null)
                {
                    Log.Error($"Could not find subscription id");
                    return false;
                }

                _activeSubscriptions.Add(subscriptionIdToken.ToObject<string>());
                return true;
            }

            string userId = tokenValidationResponse.UserID;

            string broadcasterId = userId;
            string broadcasterName = tokenValidationResponse.Username;

            if (!string.IsNullOrEmpty(_overrideBroadcasterName))
            {
                Result<GetUsersResponse> getUsersResult = await StaticTwitchAPI.GetUsers(_accessToken, [], [_overrideBroadcasterName], cancellationToken).ConfigureAwait(false);
                if (getUsersResult.IsSuccess)
                {
                    broadcasterName = _overrideBroadcasterName;
                    broadcasterId = getUsersResult.Value.Users[0].UserId;
                }
                else
                {
                    Log.Error($"Failed to retrieve override broadcaster data {getUsersResult.Exception}");
                }
            }

            ConnectedToChannel = broadcasterName;

            bool allConnectedSuccessfully = true;

            allConnectedSuccessfully &= await sendSubscription(new
            {
                type = "channel.chat.message",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = broadcasterId,
                    user_id = userId
                },
                transport = new
                {
                    method = "websocket",
                    session_id = _sessionID
                }
            }, cancellationToken).ConfigureAwait(false);

            allConnectedSuccessfully &= await sendSubscription(new
            {
                type = "channel.chat.notification",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = broadcasterId,
                    user_id = userId
                },
                transport = new
                {
                    method = "websocket",
                    session_id = _sessionID
                }
            }, cancellationToken).ConfigureAwait(false);

            if (allConnectedSuccessfully)
            {
                OnFullyConnected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                OnConnectionError?.Invoke(this, new ConnectionErrorEventArgs(ConnectionErrorType.FailedEventSubscribe));
            }
        }

        void handleNotificationMessage(JToken jsonObject)
        {
            JToken subscriptionTypeToken = jsonObject.SelectToken("payload.subscription.type", false);
            if (subscriptionTypeToken == null)
            {
                Log.Error("Failed to find subscription type");
                return;
            }

            string subscriptionType = subscriptionTypeToken.ToObject<string>();
            switch (subscriptionType)
            {
                case "channel.chat.message":
                    handleChannelChatMessageNotification(jsonObject);
                    break;
                case "channel.chat.notification":
                    handleChannelChatNotificationNotification(jsonObject);
                    break;
                default:
                    Log.Warning($"Unhandled notification message: {subscriptionType}");
                    break;
            }
        }

        void handleChannelChatMessageNotification(JToken jsonObject)
        {
            if (OnChannelChatMessage == null)
                return;

            JToken eventToken = jsonObject.SelectToken("payload.event");
            if (eventToken == null)
            {
                Log.Error("Failed to find event object");
                return;
            }

            ChannelChatMessageEvent chatMessageEvent;
            try
            {
                chatMessageEvent = eventToken.ToObject<ChannelChatMessageEvent>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize chat message: {e}");
                return;
            }

            OnChannelChatMessage?.Invoke(this, chatMessageEvent);
        }

        void handleChannelChatNotificationNotification(JToken jsonObject)
        {
            if (OnChannelChatNotification == null)
                return;

            JToken eventToken = jsonObject.SelectToken("payload.event");
            if (eventToken == null)
            {
                Log.Error("Failed to find event object");
                return;
            }

            ChannelChatNotificationEvent chatNotificationEvent;
            try
            {
                chatNotificationEvent = eventToken.ToObject<ChannelChatNotificationEvent>();
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize chat notification: {e}");
                return;
            }

            OnChannelChatNotification?.Invoke(this, chatNotificationEvent);
        }
    }
}
