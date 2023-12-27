using HG;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.SkillSlots
{
    [ValueModificationManager]
    public class SkillSlotModificationManager : NetworkedValueModificationManager<SkillSlotModificationData>
    {
        static SkillSlotModificationManager _instance;
        public static SkillSlotModificationManager Instance => _instance;

        public const int SKILL_SLOT_COUNT = (int)SkillSlot.Special + 1;

        public delegate void SkillSlotLockedDelegate(SkillSlot slot);

        public static event SkillSlotLockedDelegate OnSkillSlotLocked;
        public static event SkillSlotLockedDelegate OnSkillSlotUnlocked;

        static uint getSlotBitMask(SkillSlot skillSlot)
        {
            sbyte skillSlotLockedBit = (sbyte)skillSlot;
            if (skillSlotLockedBit < 0 || skillSlotLockedBit >= sizeof(uint) * 8)
                return 0U;

            return 1U << skillSlotLockedBit;
        }

        static bool isSkillSlotBitSet(uint mask, SkillSlot skillSlot)
        {
            uint lockedBitMask = getSlotBitMask(skillSlot);
            if (lockedBitMask == 0U)
                return false;

            return (mask & lockedBitMask) != 0;
        }

        const uint LOCKED_SKILL_SLOTS_DIRTY_BIT = 1 << 1;

        uint _lockedSkillSlotsMask;
        public uint NetworkLockedSkillSlotsMask
        {
            get
            {
                return _lockedSkillSlotsMask;
            }

            [param: In]
            set
            {
                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncLockedSkillSlots(value);
                    syncVarHookGuard = false;
                }

                SetSyncVar(value, ref _lockedSkillSlotsMask, LOCKED_SKILL_SLOTS_DIRTY_BIT);
            }
        }

        SkillSlot[] _nonLockedSkillSlots = Array.Empty<SkillSlot>();
        public SkillSlot[] NonLockedSkillSlots
        {
            get
            {
                return _nonLockedSkillSlots;
            }
            private set
            {
                _nonLockedSkillSlots = value;
                refreshNonLockedNonForceActivatedSkillSlots();
            }
        }

        void syncLockedSkillSlots(uint lockedSkillSlotsMask)
        {
            // Creates mask where 1's mark any change in the locked skills mask
            uint changedSlotsMask = NetworkLockedSkillSlotsMask ^ lockedSkillSlotsMask;

            NetworkLockedSkillSlotsMask = lockedSkillSlotsMask;

            List<SkillSlot> nonLockedSkillSlots = new List<SkillSlot>(SKILL_SLOT_COUNT);

            for (SkillSlot i = 0; i < (SkillSlot)SKILL_SLOT_COUNT; i++)
            {
                if (!IsSkillSlotLocked(i))
                {
                    nonLockedSkillSlots.Add(i);
                }
            }

            NonLockedSkillSlots = nonLockedSkillSlots.ToArray();

            for (SkillSlot i = 0; i < (SkillSlot)SKILL_SLOT_COUNT; i++)
            {
                if (isSkillSlotBitSet(changedSlotsMask, i)) // If this slot was changed
                {
                    if (isSkillSlotBitSet(lockedSkillSlotsMask, i))
                    {
                        // Slot was changed to being locked
                        OnSkillSlotLocked?.Invoke(i);
                    }
                    else
                    {
                        // Slot was changed to being unlocked
                        OnSkillSlotUnlocked?.Invoke(i);
                    }
                }
            }
        }

        public bool IsSkillSlotLocked(SkillSlot skillSlot)
        {
            return isSkillSlotBitSet(_lockedSkillSlotsMask, skillSlot);
        }

        const uint FORCE_ACTIVATE_SKILL_SLOTS_MASK_DIRTY_BIT = 1u << 2;

        uint _forceActivateSkillSlotsMask;

        public uint NetworkForceActivateSkillSlotsMask
        {
            get
            {
                return _forceActivateSkillSlotsMask;
            }

            [param: In]
            set
            {
                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncForceActivatedSkillSlots(value);
                    syncVarHookGuard = false;
                }

                SetSyncVar(value, ref _forceActivateSkillSlotsMask, FORCE_ACTIVATE_SKILL_SLOTS_MASK_DIRTY_BIT);
            }
        }

        SkillSlot[] _nonForceActivatedSkillSlots = Array.Empty<SkillSlot>();
        public SkillSlot[] NonForceActivatedSkillSlots
        {
            get
            {
                return _nonForceActivatedSkillSlots;
            }
            private set
            {
                _nonForceActivatedSkillSlots = value;
                refreshNonLockedNonForceActivatedSkillSlots();
            }
        }

        void syncForceActivatedSkillSlots(uint forceActivatedSkillSlotsMask)
        {
            NetworkForceActivateSkillSlotsMask = forceActivatedSkillSlotsMask;

            List<SkillSlot> nonForceActivatedSkillSlots = new List<SkillSlot>(SKILL_SLOT_COUNT);

            for (SkillSlot i = 0; i < (SkillSlot)SKILL_SLOT_COUNT; i++)
            {
                if (!IsSkillSlotForceActivated(i))
                {
                    nonForceActivatedSkillSlots.Add(i);
                }
            }

            NonForceActivatedSkillSlots = nonForceActivatedSkillSlots.ToArray();
        }

        public bool IsSkillSlotForceActivated(SkillSlot skillSlot)
        {
            return isSkillSlotBitSet(_forceActivateSkillSlotsMask, skillSlot);
        }

        public SkillSlot[] NonLockedNonForceActivatedSkillSlots { get; private set; } = Array.Empty<SkillSlot>();

        void refreshNonLockedNonForceActivatedSkillSlots()
        {
            NonLockedNonForceActivatedSkillSlots = NonLockedSkillSlots.Intersect(NonForceActivatedSkillSlots).ToArray();
        }

        const uint SKILL_SLOT_COOLDOWN_SCALES_DIRTY_BIT = 1u << 3;

        float[] _skillSlotCooldownScales = new float[SKILL_SLOT_COUNT];
        public float[] NetworkSkillSlotCooldownScales
        {
            get
            {
                return _skillSlotCooldownScales;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                if (value.Length != SKILL_SLOT_COUNT)
                {
                    Log.Error($"Cooldown scales array must have a size of exactly {SKILL_SLOT_COUNT}");
                    return;
                }

                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncNetworkSkillSlotCooldownScales(value);
                    syncVarHookGuard = false;
                }

                if (!ArrayUtil.ElementsEqual(_skillSlotCooldownScales, value))
                {
                    SetDirtyBit(SKILL_SLOT_COOLDOWN_SCALES_DIRTY_BIT);
                    _skillSlotCooldownScales = value;
                }
            }
        }

        public float GetCooldownScale(SkillSlot skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= (SkillSlot)SKILL_SLOT_COUNT)
                return 1f;

            return _skillSlotCooldownScales[(int)skillSlot];
        }

        void syncNetworkSkillSlotCooldownScales(float[] cooldownScales)
        {
            NetworkSkillSlotCooldownScales = cooldownScales;

            foreach (GenericSkillTracker skillTracker in InstanceTracker.GetInstancesList<GenericSkillTracker>())
            {
                if (skillTracker.Skill)
                {
                    skillTracker.Skill.RecalculateValues();
                }
            }
        }

        const uint SKILL_SLOT_STOCK_ADDS_DIRTY_BIT = 1u << 4;

        int[] _skillSlotStockAdds = new int[SKILL_SLOT_COUNT];
        public int[] NetworkSkillSlotStockAdds
        {
            get
            {
                return _skillSlotStockAdds;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                if (value.Length != SKILL_SLOT_COUNT)
                {
                    Log.Error($"Stock adds array must have a size of exactly {SKILL_SLOT_COUNT}");
                    return;
                }

                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncNetworkSkillSlotStockAdds(value);
                    syncVarHookGuard = false;
                }

                if (!ArrayUtil.ElementsEqual(_skillSlotStockAdds, value))
                {
                    SetDirtyBit(SKILL_SLOT_STOCK_ADDS_DIRTY_BIT);
                    _skillSlotStockAdds = value;
                }
            }
        }

        public int GetStockAdd(SkillSlot skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= (SkillSlot)SKILL_SLOT_COUNT)
                return 0;

            return _skillSlotStockAdds[(int)skillSlot];
        }

        void syncNetworkSkillSlotStockAdds(int[] stockAdds)
        {
            NetworkSkillSlotStockAdds = stockAdds;

            foreach (GenericSkillTracker skillTracker in InstanceTracker.GetInstancesList<GenericSkillTracker>())
            {
                if (skillTracker.Skill)
                {
                    skillTracker.Skill.RecalculateValues();
                }
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncLockedSkillSlots(_lockedSkillSlotsMask);
            syncForceActivatedSkillSlots(_forceActivateSkillSlotsMask);
            syncNetworkSkillSlotCooldownScales(_skillSlotCooldownScales);
            syncNetworkSkillSlotStockAdds(_skillSlotStockAdds);
        }

        protected override void Awake()
        {
            base.Awake();

            ArrayUtils.SetAll(_skillSlotCooldownScales, 1f);
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

        public override SkillSlotModificationData InterpolateValue(in SkillSlotModificationData a, in SkillSlotModificationData b, float t)
        {
            return SkillSlotModificationData.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            uint forceLockedSkillSlotsMask = 0;
            uint forceActivateSkillSlotsMask = 0;

            float[] skillCooldownScales = new float[SKILL_SLOT_COUNT];
            int[] skillStockAdds = new int[SKILL_SLOT_COUNT];

            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                SkillSlotModificationData modificationData = GetModifiedValue(new SkillSlotModificationData((SkillSlot)i));

                uint maskBit = getSlotBitMask(modificationData.SlotIndex);

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

            NetworkLockedSkillSlotsMask = forceLockedSkillSlotsMask;
            NetworkForceActivateSkillSlotsMask = forceActivateSkillSlotsMask;
            NetworkSkillSlotCooldownScales = skillCooldownScales;
            NetworkSkillSlotStockAdds = skillStockAdds;
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool result = base.serialize(writer, initialState, dirtyBits);
            if (initialState)
            {
                writer.WritePackedUInt32(_lockedSkillSlotsMask);
                writer.WritePackedUInt32(_forceActivateSkillSlotsMask);

                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    writer.Write(_skillSlotCooldownScales[i]);
                }

                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    writer.Write(_skillSlotStockAdds[i]);
                }

                return result;
            }

            bool anythingWritten = false;

            if ((dirtyBits & LOCKED_SKILL_SLOTS_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32(_lockedSkillSlotsMask);
                anythingWritten = true;
            }

            if ((dirtyBits & FORCE_ACTIVATE_SKILL_SLOTS_MASK_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32(_forceActivateSkillSlotsMask);
                anythingWritten = true;
            }

            if ((dirtyBits & SKILL_SLOT_COOLDOWN_SCALES_DIRTY_BIT) != 0)
            {
                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    writer.Write(_skillSlotCooldownScales[i]);
                }

                anythingWritten = true;
            }

            if ((dirtyBits & SKILL_SLOT_STOCK_ADDS_DIRTY_BIT) != 0)
            {
                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    writer.Write(_skillSlotStockAdds[i]);
                }

                anythingWritten = true;
            }

            return result || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _lockedSkillSlotsMask = reader.ReadPackedUInt32();
                _forceActivateSkillSlotsMask = reader.ReadPackedUInt32();

                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    _skillSlotCooldownScales[i] = reader.ReadSingle();
                }

                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    _skillSlotStockAdds[i] = reader.ReadInt32();
                }

                return;
            }

            if ((dirtyBits & LOCKED_SKILL_SLOTS_DIRTY_BIT) != 0)
            {
                syncLockedSkillSlots(reader.ReadPackedUInt32());
            }

            if ((dirtyBits & FORCE_ACTIVATE_SKILL_SLOTS_MASK_DIRTY_BIT) != 0)
            {
                syncForceActivatedSkillSlots(reader.ReadPackedUInt32());
            }

            if ((dirtyBits & SKILL_SLOT_COOLDOWN_SCALES_DIRTY_BIT) != 0)
            {
                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    _skillSlotCooldownScales[i] = reader.ReadSingle();
                }

                syncNetworkSkillSlotCooldownScales(_skillSlotCooldownScales);
            }

            if ((dirtyBits & SKILL_SLOT_STOCK_ADDS_DIRTY_BIT) != 0)
            {
                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    _skillSlotStockAdds[i] = reader.ReadInt32();
                }

                syncNetworkSkillSlotStockAdds(_skillSlotStockAdds);
            }
        }
    }
}
