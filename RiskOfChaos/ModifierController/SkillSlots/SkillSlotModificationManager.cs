using RoR2;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.SkillSlots
{
    public class SkillSlotModificationManager : NetworkBehaviour
    {
        static SkillSlotModificationManager _instance;
        public static SkillSlotModificationManager Instance => _instance;

        public const int SKILL_SLOT_COUNT = (int)SkillSlot.Special + 1;

        readonly HashSet<ISkillSlotModificationProvider> _modificationProviders = new HashSet<ISkillSlotModificationProvider>();

        const uint LOCKED_SKILL_SLOTS_DIRTY_BIT = 1 << 0;

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

        public SkillSlot[] NonLockedSkillSlots { get; private set; }

        void syncLockedSkillSlots(uint lockedSkillSlotsMask)
        {
            NetworkLockedSkillSlotsMask = lockedSkillSlotsMask;

            List<SkillSlot> nonLockedSkillSlots = new List<SkillSlot>();

            for (SkillSlot i = 0; i < (SkillSlot)SKILL_SLOT_COUNT; i++)
            {
                if (!IsSkillSlotLocked(i))
                {
                    nonLockedSkillSlots.Add(i);
                }
            }

            NonLockedSkillSlots = nonLockedSkillSlots.ToArray();
        }

        static uint getLockedBitMask(SkillSlot skillSlot)
        {
            sbyte skillSlotLockedBit = (sbyte)skillSlot;
            if (skillSlotLockedBit < 0 || skillSlotLockedBit >= sizeof(uint) * 8)
                return 0U;

            return 1U << skillSlotLockedBit;
        }

        public bool IsSkillSlotLocked(SkillSlot skillSlot)
        {
            uint lockedBitMask = getLockedBitMask(skillSlot);
            if (lockedBitMask == 0U)
                return false;

            return (_lockedSkillSlotsMask & lockedBitMask) != 0;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncLockedSkillSlots(_lockedSkillSlotsMask);
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        public void RegisterModificationProvider(ISkillSlotModificationProvider provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Add(provider))
            {
                updateSlotModifications();
            }
        }

        public void UnregisterModificationProvider(ISkillSlotModificationProvider provider)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_modificationProviders.Remove(provider))
            {
                updateSlotModifications();
            }
        }

        void updateSlotModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            SkillSlotModificationData[] modificationDatas = new SkillSlotModificationData[SKILL_SLOT_COUNT];
            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                modificationDatas[i] = new SkillSlotModificationData((SkillSlot)i);
            }

            foreach (ISkillSlotModificationProvider modificationProvider in _modificationProviders)
            {
                for (int i = 0; i < SKILL_SLOT_COUNT; i++)
                {
                    modificationProvider.ModifySkillSlot(ref modificationDatas[i]);
                }
            }

            uint forceLockedSkillSlotsMask = 0;

            for (int i = 0; i < SKILL_SLOT_COUNT; i++)
            {
                SkillSlotModificationData modificationData = modificationDatas[i];

                if (modificationData.ForceIsLocked)
                {
                    forceLockedSkillSlotsMask |= 1U << i;
                }
            }

            NetworkLockedSkillSlotsMask = forceLockedSkillSlotsMask;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WritePackedUInt32(_lockedSkillSlotsMask);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & LOCKED_SKILL_SLOTS_DIRTY_BIT) != 0)
            {
                writer.WritePackedUInt32(_lockedSkillSlotsMask);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _lockedSkillSlotsMask = reader.ReadPackedUInt32();
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & LOCKED_SKILL_SLOTS_DIRTY_BIT) != 0)
            {
                syncLockedSkillSlots(reader.ReadPackedUInt32());
            }
        }
    }
}
