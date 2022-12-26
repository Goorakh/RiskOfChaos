using R2API.Networking;
using R2API.Networking.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public class SyncSetGravity : INetMessage
    {
        public delegate void OnReceiveDelegate(in Vector3 newGravity);
        public static event OnReceiveDelegate OnReceive;

        public static Vector3 NetworkedGravity
        {
            get => Physics.gravity;
            [param: In]
            set
            {
                SetGravityNetworked(value);
            }
        }

        Vector3 _newGravity;

        public SyncSetGravity(Vector3 newGravity)
        {
            _newGravity = newGravity;
        }

        public SyncSetGravity()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetGravityNetworked(in Vector3 newGravity)
        {
            if (NetworkClient.active)
            {
                new SyncSetGravity(newGravity).Send(NetworkDestination.Clients | NetworkDestination.Server);
            }
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_newGravity);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _newGravity = reader.ReadVector3();
        }

        void INetMessage.OnReceived()
        {
            const string LOG_PREFIX = $"{nameof(SyncSetGravity)}.{nameof(INetMessage.OnReceived)} ";

#if DEBUG
            Log.Debug(LOG_PREFIX + "Received");
#endif

            Physics.gravity = _newGravity;

            OnReceive?.Invoke(_newGravity);
        }
    }
}
