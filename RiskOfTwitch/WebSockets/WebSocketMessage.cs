using System;
using System.Net.WebSockets;

namespace RiskOfTwitch.WebSockets
{
    internal readonly struct WebSocketMessage
    {
        public readonly ArraySegment<byte> MessageData;

        public readonly WebSocketCloseStatus? CloseStatus;

        public readonly string CloseStatusDescription;

        public readonly WebSocketMessageType MessageType;

        public WebSocketMessage(ArraySegment<byte> messageData, WebSocketReceiveResult result)
        {
            MessageData = messageData;

            CloseStatus = result.CloseStatus;
            CloseStatusDescription = result.CloseStatusDescription;
            MessageType = result.MessageType;
        }
    }
}
