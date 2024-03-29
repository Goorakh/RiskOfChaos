﻿using RiskOfChaos.Utilities.Interpolation;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.AttackDelay
{
    [ValueModificationManager]
    public class AttackDelayModificationManager : NetworkedValueModificationManager<AttackDelayModificationInfo>
    {
        static AttackDelayModificationManager _instance;
        public static AttackDelayModificationManager Instance => _instance;

        const uint TOTAL_ATTACK_DELAY_DIRTY_BIT = 1 << 1;

        float _totalAttackDelay = 0f;
        public float NetworkedTotalAttackDelay
        {
            get
            {
                return _totalAttackDelay;
            }
            set
            {
                SetSyncVar(value, ref _totalAttackDelay, TOTAL_ATTACK_DELAY_DIRTY_BIT);
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

        public override AttackDelayModificationInfo InterpolateValue(in AttackDelayModificationInfo a, in AttackDelayModificationInfo b, float t)
        {
            return AttackDelayModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            AttackDelayModificationInfo attackDelayModificationInfo = GetModifiedValue(new AttackDelayModificationInfo(0f));
            NetworkedTotalAttackDelay = attackDelayModificationInfo.TotalDelay;
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseAnythingWritten = base.serialize(writer, initialState, dirtyBits);

            if (initialState)
            {
                writer.Write(_totalAttackDelay);
                return true;
            }

            bool anythingWritten = false;

            if ((dirtyBits & TOTAL_ATTACK_DELAY_DIRTY_BIT) != 0)
            {
                writer.Write(_totalAttackDelay);
                anythingWritten = true;
            }

            return baseAnythingWritten || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _totalAttackDelay = reader.ReadSingle();
                return;
            }

            if ((dirtyBits & TOTAL_ATTACK_DELAY_DIRTY_BIT) != 0)
            {
                _totalAttackDelay = reader.ReadSingle();
            }
        }
    }
}
