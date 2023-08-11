using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.AttackDelay
{
    public class AttackDelayModificationManager : NetworkedValueModificationManager<IAttackDelayModificationProvider, AttackDelayModificationInfo>
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

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        protected override void updateValueModifications()
        {
            AttackDelayModificationInfo attackDelayModificationInfo = getModifiedValue(new AttackDelayModificationInfo(0f));
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
