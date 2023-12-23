using System.Runtime.InteropServices;
using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Knockback
{
    public class KnockbackModificationManager : NetworkedValueModificationManager<float>
    {
        static KnockbackModificationManager _instance;
        public static KnockbackModificationManager Instance => _instance;

        const uint TOTAL_KNOCKBACK_MULTIPLIER_DIRTY_BIT = 1 << 1;

        float _totalKnockbackMultiplier = 1f;
        public float NetworkedTotalKnockbackMultiplier
        {
            get
            {
                return _totalKnockbackMultiplier;
            }

            [param: In]
            set
            {
                SetSyncVar(value, ref _totalKnockbackMultiplier, TOTAL_KNOCKBACK_MULTIPLIER_DIRTY_BIT);
            }
        }

        public override float InterpolateValue(in float a, in float b, float t)
        {
            return ValueInterpolationFunctionType.Linear.Interpolate(a, b, t);
        }

        public override void UpdateValueModifications()
        {
            NetworkedTotalKnockbackMultiplier = GetModifiedValue(1f);
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

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseResult = base.serialize(writer, initialState, dirtyBits);
            if (initialState)
            {
                writer.Write(_totalKnockbackMultiplier);
                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & TOTAL_KNOCKBACK_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_totalKnockbackMultiplier);
                anythingWritten = true;
            }

            return baseResult || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _totalKnockbackMultiplier = reader.ReadSingle();
                return;
            }

            if ((dirtyBits & TOTAL_KNOCKBACK_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _totalKnockbackMultiplier = reader.ReadSingle();
            }
        }
    }
}
