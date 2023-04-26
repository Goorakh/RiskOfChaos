using RiskOfChaos.Utilities;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncTimeScale : NetworkBehaviour
    {
        const uint TIME_SCALE_DIRTY_BIT = 1 << 0;

        float _timeScale;
        public float NetworkedTimeScale
        {
            get
            {
                return _timeScale;
            }

            [param: In]
            set
            {
                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncTimeScale(value);
                    syncVarHookGuard = false;
                }

                SetSyncVar(value, ref _timeScale, TIME_SCALE_DIRTY_BIT);
            }
        }

        void syncTimeScale(float value)
        {
            NetworkedTimeScale = value;
            TimeUtils.UnpausedTimeScale = value;
        }

        void Awake()
        {
            _timeScale = TimeUtils.UnpausedTimeScale;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncTimeScale(_timeScale);
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            NetworkedTimeScale = TimeUtils.UnpausedTimeScale;
        }

        void FixedUpdate()
        {
            float timeScale = TimeUtils.UnpausedTimeScale;
            if (_timeScale != timeScale)
            {
                if (hasAuthority)
                {
                    NetworkedTimeScale = timeScale;
                }
                else
                {
                    TimeUtils.UnpausedTimeScale = _timeScale;
                }
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_timeScale);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & TIME_SCALE_DIRTY_BIT) != 0)
            {
                writer.Write(_timeScale);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _timeScale = reader.ReadSingle();
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();
            if ((dirtyBits & TIME_SCALE_DIRTY_BIT) != 0)
            {
                syncTimeScale(reader.ReadSingle());
            }
        }
    }
}
