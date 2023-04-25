using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Knockback
{
    public class KnockbackModificationManager : ValueModificationManager<IKnockbackModificationProvider, float>
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

        protected override void updateValueModifications()
        {
            NetworkedTotalKnockbackMultiplier = getModifiedValue(1f);
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
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
