using RoR2;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncWorldGravity : NetworkBehaviour
    {
        const uint GRAVITY_DIRTY_BIT = 1 << 0;

        Vector3 _gravity;
        public Vector3 NetworkGravity
        {
            get
            {
                return _gravity;
            }

            [param: In]
            set
            {
                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    setGravity(value);
                    syncVarHookGuard = false;
                }

                SetSyncVar(value, ref _gravity, GRAVITY_DIRTY_BIT);
            }
        }

        void Awake()
        {
            _gravity = Physics.gravity;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            setGravity(NetworkGravity);
        }

        void FixedUpdate()
        {
            Vector3 currentGravity = Physics.gravity;
            if (NetworkGravity != currentGravity)
            {
                if (NetworkServer.active)
                {
                    NetworkGravity = currentGravity;
                }
                else if (NetworkClient.active)
                {
                    Physics.gravity = NetworkGravity;
                }
            }
        }

        void OnDestroy()
        {
            setGravity(new Vector3(0f, Run.baseGravity, 0f));
        }

        void setGravity(in Vector3 gravity)
        {
#if DEBUG
            if (Physics.gravity != gravity)
            {
                Log.Debug($"New gravity: {gravity}");
            }
#endif

            NetworkGravity = gravity;
            Physics.gravity = gravity;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_gravity);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            if ((dirtyBits & GRAVITY_DIRTY_BIT) != 0)
            {
                writer.Write(_gravity);
            }

            return dirtyBits != 0b0;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                setGravity(reader.ReadVector3());
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & GRAVITY_DIRTY_BIT) != 0)
            {
                setGravity(reader.ReadVector3());
            }
        }
    }
}
