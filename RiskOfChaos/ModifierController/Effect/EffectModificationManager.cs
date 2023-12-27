using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Effect
{
    [ValueModificationManager]
    public sealed class EffectModificationManager : NetworkedValueModificationManager<EffectModificationInfo>
    {
        static EffectModificationManager _instance;
        public static EffectModificationManager Instance => _instance;

        float _durationMultiplier = 1f;
        const uint DURATION_MULTIPLIER_DIRTY_BIT = 1 << 1;

        public float DurationMultiplier
        {
            get
            {
                return _durationMultiplier;
            }
            set
            {
                SetSyncVar(value, ref _durationMultiplier, DURATION_MULTIPLIER_DIRTY_BIT);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);
        }

        public override EffectModificationInfo InterpolateValue(in EffectModificationInfo a, in EffectModificationInfo b, float t)
        {
            return EffectModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            EffectModificationInfo modificationInfo = GetModifiedValue(new EffectModificationInfo());

            DurationMultiplier = modificationInfo.DurationMultiplier;
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseResult = base.serialize(writer, initialState, dirtyBits);

            if (initialState)
            {
                writer.Write(_durationMultiplier);

                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & DURATION_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_durationMultiplier);
                anythingWritten = true;
            }

            return baseResult || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _durationMultiplier = reader.ReadSingle();
                return;
            }

            if ((dirtyBits & DURATION_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _durationMultiplier = reader.ReadSingle();
            }
        }
    }
}
