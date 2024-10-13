using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.OLD_ModifierController.SkillSlots
{
    [ValueModificationManager(typeof(SyncSkillSlotModification))]
    public class SkillSlotModificationManager : ValueModificationManager<SkillSlotModificationData>
    {
        static SkillSlotModificationManager _instance;
        public static SkillSlotModificationManager Instance => _instance;

        public const int SKILL_SLOT_COUNT = (int)SkillSlot.Special + 1;

        public delegate void SkillSlotLockedDelegate(SkillSlot slot);

        public static event SkillSlotLockedDelegate OnSkillSlotLocked;
        public static event SkillSlotLockedDelegate OnSkillSlotUnlocked;

        public static uint GetSlotBitMask(SkillSlot skillSlot)
        {
            sbyte skillSlotLockedBit = (sbyte)skillSlot;
            if (skillSlotLockedBit < 0 || skillSlotLockedBit >= sizeof(uint) * 8)
                return 0U;

            return 1U << skillSlotLockedBit;
        }

        public static bool IsSkillSlotBitSet(uint mask, SkillSlot skillSlot)
        {
            uint lockedBitMask = GetSlotBitMask(skillSlot);
            if (lockedBitMask == 0U)
                return false;

            return (mask & lockedBitMask) != 0;
        }

        SyncSkillSlotModification _clientSync;

        public uint LockedSkillSlotsMask
        {
            get
            {
                return _clientSync.LockedSkillSlotsMask;
            }
            private set
            {
                _clientSync.LockedSkillSlotsMask = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSkillSlotLocked(SkillSlot skillSlot)
        {
            return IsSkillSlotBitSet(LockedSkillSlotsMask, skillSlot);
        }

        public uint ForceActivateSkillSlotsMask
        {
            get
            {
                return _clientSync.ForceActivateSkillSlotsMask;
            }
            private set
            {
                _clientSync.ForceActivateSkillSlotsMask = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSkillSlotForceActivated(SkillSlot skillSlot)
        {
            return IsSkillSlotBitSet(ForceActivateSkillSlotsMask, skillSlot);
        }

        public float[] SkillSlotCooldownScales
        {
            get
            {
                return _clientSync.CooldownScales;
            }
            private set
            {
                _clientSync.CooldownScales = value;
            }
        }

        public float GetCooldownScale(SkillSlot skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= (SkillSlot)SKILL_SLOT_COUNT)
                return 1f;

            return SkillSlotCooldownScales[(int)skillSlot];
        }

        public int[] SkillSlotStockAdds
        {
            get
            {
                return _clientSync.StockAdds;
            }
            private set
            {
                _clientSync.StockAdds = value;
            }
        }

        public int GetStockAdd(SkillSlot skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= (SkillSlot)SKILL_SLOT_COUNT)
                return 0;

            return SkillSlotStockAdds[(int)skillSlot];
        }

        protected override void Awake()
        {
            base.Awake();
            _clientSync = GetComponent<SyncSkillSlotModification>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);

            _clientSync.OnSkillSlotLocked += _clientSync_OnSkillSlotLocked;
            _clientSync.OnSkillSlotUnlocked += _clientSync_OnSkillSlotUnlocked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            _clientSync.OnSkillSlotLocked -= _clientSync_OnSkillSlotLocked;
            _clientSync.OnSkillSlotUnlocked -= _clientSync_OnSkillSlotUnlocked;

            SingletonHelper.Unassign(ref _instance, this);
        }

        void _clientSync_OnSkillSlotLocked(SkillSlot slot)
        {
            OnSkillSlotLocked?.Invoke(slot);
        }

        void _clientSync_OnSkillSlotUnlocked(SkillSlot slot)
        {
            OnSkillSlotUnlocked?.Invoke(slot);
        }

        public override SkillSlotModificationData InterpolateValue(in SkillSlotModificationData a, in SkillSlotModificationData b, float t)
        {
            return SkillSlotModificationData.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            uint forceLockedSkillSlotsMask = 0;
            uint forceActivateSkillSlotsMask = 0;

            float[] skillCooldownScales = new float[SKILL_SLOT_COUNT];
            int[] skillStockAdds = new int[SKILL_SLOT_COUNT];

            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                SkillSlotModificationData modificationData = GetModifiedValue(new SkillSlotModificationData((SkillSlot)i));

                uint maskBit = GetSlotBitMask(modificationData.SlotIndex);

                if (modificationData.ForceIsLocked)
                {
                    forceLockedSkillSlotsMask |= maskBit;
                }

                if (modificationData.ForceActivate)
                {
                    forceActivateSkillSlotsMask |= maskBit;
                }

                skillCooldownScales[i] = modificationData.CooldownScale;
                skillStockAdds[i] = modificationData.StockAdds;
            }

            LockedSkillSlotsMask = forceLockedSkillSlotsMask;
            ForceActivateSkillSlotsMask = forceActivateSkillSlotsMask;
            SkillSlotCooldownScales = skillCooldownScales;
            SkillSlotStockAdds = skillStockAdds;
        }
    }
}
