using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public static class NetworkMessageQueue
    {
        sealed class QueuedMessage
        {
            public readonly INetMessage Message;

            readonly NetworkConnection _targetConnection;
            readonly NetworkDestination _destination;

            public QueuedMessage(INetMessage message, NetworkConnection targetConnection)
            {
                Message = message ?? throw new ArgumentNullException(nameof(message));
                _targetConnection = targetConnection ?? throw new ArgumentNullException(nameof(targetConnection));
            }

            public QueuedMessage(INetMessage message, NetworkDestination destination)
            {
                Message = message ?? throw new ArgumentNullException(nameof(message));
                _destination = destination;
            }

            public void Send()
            {
                if (_targetConnection != null)
                {
                    Message.Send(_targetConnection);
                }
                else
                {
                    Message.Send(_destination);
                }
            }
        }

        static readonly Queue<QueuedMessage> _messageQueue = [];

        static bool hasNetworkSession => PlatformSystems.networkManager && PlatformSystems.networkManager.isNetworkActive;

        [SystemInitializer]
        static void Init()
        {
            RoR2Application.onFixedUpdate += onFixedUpdate;
            NetworkManagerSystem.onStartGlobal += onNetworkManagerStartGlobal;
        }

        static void onNetworkManagerStartGlobal()
        {
            _messageQueue.Clear();
            _messageQueue.TrimExcess();
        }

        static void onFixedUpdate()
        {
            if (!hasNetworkSession)
                return;
            
            while (_messageQueue.TryDequeue(out QueuedMessage message))
            {
                message.Send();
            }
        }

        public static void EnqueueMessage(INetMessage message, NetworkDestination destination)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            if (!hasNetworkSession)
            {
                Log.Warning($"Cannot enqueue message without an active network session (msg=[{message}], destination=[{destination}])");
                return;
            }

            _messageQueue.Enqueue(new QueuedMessage(message, destination));
        }

        public static void EnqueueMessage(INetMessage message, NetworkConnection targetConnection)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            if (targetConnection is null)
                throw new ArgumentNullException(nameof(targetConnection));

            if (!hasNetworkSession)
            {
                Log.Warning($"Cannot enqueue message without an active network session (msg=[{message}], target=[{targetConnection}])");
                return;
            }

            _messageQueue.Enqueue(new QueuedMessage(message, targetConnection));
        }
    }
}
