using R2API.Networking.Interfaces;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public sealed class EnsureNetworkDestroy : MonoBehaviour
    {
        sealed class NetworkObjectInfo
        {
            public readonly NetworkInstanceId NetId;
            public readonly string ObjectIdentifier;

            readonly List<NetworkConnection> _remainingClientConnections = [];

            int _numRequestsSent = 0;
            float _lastRequestSendTime = float.NegativeInfinity;

            public NetworkObjectInfo(NetworkInstanceId netId, string objectIdentifier)
            {
                NetId = netId;
                ObjectIdentifier = objectIdentifier;

                _remainingClientConnections.EnsureCapacity(NetworkServer.connections.Count);
                foreach (NetworkConnection clientConnection in NetworkServer.connections)
                {
                    if (clientConnection != null && clientConnection.isConnected)
                    {
                        _remainingClientConnections.Add(clientConnection);
                    }
                }

                _remainingClientConnections.TrimExcess();
            }

            public bool ShouldSendRequest => _numRequestsSent <= 0 || Time.unscaledTime - _lastRequestSendTime > 1f;

            public bool HasAnyRemainingConfirmations => _remainingClientConnections.Count > 0;

            public void SendConfirmationRequest()
            {
                if (!NetworkServer.active)
                {
                    Log.Warning("Called on client");
                    return;
                }

                foreach (NetworkConnection connection in _remainingClientConnections)
                {
                    if (connection != null && connection.isReady && connection.isConnected)
                    {
                        EnsureObjectDestroyMessage ensureDestroyMessage = new EnsureObjectDestroyMessage(NetId, connection.connectionId);
                        ensureDestroyMessage.Send<EnsureObjectDestroyMessage, EnsureObjectDestroyMessage.Reply>(connection);
                    }
                }

                _lastRequestSendTime = Time.unscaledTime;
                _numRequestsSent++;

                Log.Debug($"Sending destroy confirmation request #{_numRequestsSent} for {ObjectIdentifier} (id={NetId})");
            }

            public void HandleObjectDestroyedReply(EnsureObjectDestroyMessage.Reply reply)
            {
                if (!NetworkServer.active)
                {
                    Log.Warning("Called on client");
                    return;
                }

                for (int i = _remainingClientConnections.Count - 1; i >= 0; i--)
                {
                    NetworkConnection clientConnection = _remainingClientConnections[i];
                    if (clientConnection != null && clientConnection.isConnected && clientConnection.connectionId == reply.ClientConnectionId)
                    {
                        Log.Debug($"Received destroy confirmation for {ObjectIdentifier} (id={NetId}) from connection: [{clientConnection}]");
                        _remainingClientConnections.RemoveAt(i);
                    }
                }
            }

            public override string ToString()
            {
                return $"{ObjectIdentifier} (id={NetId}), request: {_numRequestsSent}, remaining clients: {_remainingClientConnections.Count}";
            }
        }

        static readonly List<NetworkObjectInfo> _pendingDestroyConfirmations = [];

        [SystemInitializer]
        static void Init()
        {
            EnsureObjectDestroyMessage.Reply.OnReceived += onObjectDestroyReplyReceived;
            RoR2Application.onUpdate += staticUpdate;
        }

        static void onObjectDestroyReplyReceived(EnsureObjectDestroyMessage.Reply reply)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            foreach (NetworkObjectInfo pendingDestroyedObject in _pendingDestroyConfirmations)
            {
                if (pendingDestroyedObject.NetId == reply.ObjectId)
                {
                    pendingDestroyedObject.HandleObjectDestroyedReply(reply);
                }
            }
        }

        static void staticUpdate()
        {
            if (_pendingDestroyConfirmations.Count <= 0)
                return;

            if (!NetworkServer.active)
            {
                _pendingDestroyConfirmations.Clear();
                return;
            }

            for (int i = _pendingDestroyConfirmations.Count - 1; i >= 0; i--)
            {
                NetworkObjectInfo pendingDestroyConfirmation = _pendingDestroyConfirmations[i];

                if (!pendingDestroyConfirmation.HasAnyRemainingConfirmations)
                {
                    Log.Debug($"All client confirmations received for {pendingDestroyConfirmation}");
                    _pendingDestroyConfirmations.RemoveAt(i);
                    continue;
                }

                if (pendingDestroyConfirmation.ShouldSendRequest)
                {
                    pendingDestroyConfirmation.SendConfirmationRequest();
                }
            }
        }

        NetworkIdentity _networkIdentity;

        void Awake()
        {
            _networkIdentity = GetComponent<NetworkIdentity>();
            if (!_networkIdentity)
            {
                Log.Error($"{Util.GetGameObjectHierarchyName(gameObject)} is missing NetworkIdentity component");
                enabled = false;
            }
        }

        void OnDestroy()
        {
            if (NetworkServer.active)
            {
                if (_networkIdentity)
                {
                    _pendingDestroyConfirmations.Add(new NetworkObjectInfo(_networkIdentity.netId, Util.GetGameObjectHierarchyName(gameObject)));
                }
            }
        }
    }
}
