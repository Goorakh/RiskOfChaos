using RiskOfChaos.Patches;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components.Gravity
{
    public abstract class GenericSyncGravity : NetworkBehaviour
    {
        protected abstract Vector3 currentGravity { get; }

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
            _gravity = currentGravity;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            setGravity(_gravity);
        }

        void FixedUpdate()
        {
            Vector3 currentGravity = this.currentGravity;
            if (_gravity != currentGravity)
            {
                if (hasAuthority)
                {
                    NetworkGravity = currentGravity;
                }
                else
                {
                    onGravityChanged(_gravity);
                }
            }
        }

        void OnDestroy()
        {
            setGravity(GravityTracker.BaseGravity);
        }

        void setGravity(in Vector3 gravity)
        {
#if DEBUG
            if (_gravity != gravity)
            {
                Log.Debug($"New gravity: {gravity}");
            }
#endif

            NetworkGravity = gravity;
            onGravityChanged(gravity);
        }

        protected virtual void onGravityChanged(in Vector3 newGravity)
        {
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

            bool anythingWritten = false;

            if ((dirtyBits & GRAVITY_DIRTY_BIT) != 0)
            {
                writer.Write(_gravity);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _gravity = reader.ReadVector3();
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
