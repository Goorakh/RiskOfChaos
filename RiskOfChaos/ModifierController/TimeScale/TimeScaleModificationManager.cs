using RiskOfChaos.Utilities;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.TimeScale
{
    public class TimeScaleModificationManager : NetworkedValueModificationManager<ITimeScaleModificationProvider, float>
    {
        static TimeScaleModificationManager _instance;
        public static TimeScaleModificationManager Instance => _instance;

        const uint PLAYER_REALTIME_TIME_SCALE_MULTIPLIER_DIRTY_BIT = 1 << 1;

        float _playerRealtimeTimeScaleMultiplier = 1f;
        public float NetworkPlayerRealtimeTimeScaleMultiplier
        {
            get
            {
                return _playerRealtimeTimeScaleMultiplier;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _playerRealtimeTimeScaleMultiplier, PLAYER_REALTIME_TIME_SCALE_MULTIPLIER_DIRTY_BIT);
            }
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            TimeUtils.UnpausedTimeScale = 1f;
            NetworkPlayerRealtimeTimeScaleMultiplier = 1f;
        }

        protected override void updateValueModifications()
        {
            float timeScaleMultiplier = 1f;
            float playerRealtimeTimeScaleMultiplier = 1f;

            foreach (ITimeScaleModificationProvider modificationProvider in _modificationProviders)
            {
                modificationProvider.ModifyValue(ref timeScaleMultiplier);

                if (modificationProvider.ContributeToPlayerRealtimeTimeScalePatch)
                {
                    modificationProvider.ModifyValue(ref playerRealtimeTimeScaleMultiplier);
                }
            }

            TimeUtils.UnpausedTimeScale = timeScaleMultiplier;
            NetworkPlayerRealtimeTimeScaleMultiplier = playerRealtimeTimeScaleMultiplier;
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseAnythingWritten = base.serialize(writer, initialState, dirtyBits);
            if (initialState)
            {
                writer.Write(_playerRealtimeTimeScaleMultiplier);
                return true;
            }

            bool anythingWritten = false;
            if ((dirtyBits & PLAYER_REALTIME_TIME_SCALE_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_playerRealtimeTimeScaleMultiplier);
                anythingWritten = true;
            }

            return baseAnythingWritten || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _playerRealtimeTimeScaleMultiplier = reader.ReadSingle();
                return;
            }

            if ((dirtyBits & PLAYER_REALTIME_TIME_SCALE_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _playerRealtimeTimeScaleMultiplier = reader.ReadSingle();
            }
        }
    }
}
