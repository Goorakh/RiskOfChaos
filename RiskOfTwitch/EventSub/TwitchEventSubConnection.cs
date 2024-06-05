using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RiskOfTwitch.WebSockets;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiskOfTwitch.EventSub
{
    internal class TwitchEventSubConnection : ClientWebSocketConnection
    {
        readonly ITwitchEventSubMessageHandler _eventHandler;

        public TwitchEventSubConnection(Uri url, ITwitchEventSubMessageHandler eventHandler) : base(url)
        {
            _eventHandler = eventHandler;
        }

        protected override async Task handleSocketMessageAsync(WebSocketMessage message, CancellationToken cancellationToken)
        {
            if (message.MessageType == WebSocketMessageType.Text)
            {
#if DEBUG
                Log.Debug($"Received message: {Encoding.UTF8.GetString(message.MessageData.Array, message.MessageData.Offset, message.MessageData.Count)}");
#endif
            }
            else
            {
#if DEBUG
                Log.Debug($"Received message: {message.MessageData.Count} byte(s)");
#endif

                Log.Warning($"Unhandled socket message type: {message.MessageType}");

                return;
            }

            using MemoryStream memoryStream = new MemoryStream(message.MessageData.Array, message.MessageData.Offset, message.MessageData.Count);
            using StreamReader streamReader = new StreamReader(memoryStream, Encoding.UTF8);
            using JsonTextReader jsonReader = new JsonTextReader(streamReader);

            JToken deserializedMessage;
            try
            {
                deserializedMessage = await JToken.ReadFromAsync(jsonReader, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize web socket message: {e}");
                return;
            }

            await _eventHandler.HandleEventAsync(deserializedMessage, cancellationToken).ConfigureAwait(false);
        }

        protected override bool shouldReconnect(WebSocketMessage closingMessage)
        {
            // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#close-message
            if (closingMessage.CloseStatus.HasValue)
            {
                WebSocketCloseStatus closeStatus = closingMessage.CloseStatus.Value;
                if (closeStatus == (WebSocketCloseStatus)4003 || // Connection unused
                    closeStatus == (WebSocketCloseStatus)4004)   // Reconnect grace time expired
                {
                    return false;
                }
            }

            return base.shouldReconnect(closingMessage);
        }
    }
}
