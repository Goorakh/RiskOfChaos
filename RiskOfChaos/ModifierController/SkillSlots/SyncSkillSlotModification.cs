using HG;
using RiskOfChaos.Trackers;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.SkillSlots
{
    public sealed class SyncSkillSlotModification : NetworkBehaviour
    {
        [SyncVar]
        public bool AnyModificationActive;

        [SyncVar(hook = nameof(syncLockedSkillSlotsMask))]
        public uint LockedSkillSlotsMask;

        public event SkillSlotModificationManager.SkillSlotLockedDelegate OnSkillSlotLocked;
        public event SkillSlotModificationManager.SkillSlotLockedDelegate OnSkillSlotUnlocked;

        [SyncVar]
        public uint ForceActivateSkillSlotsMask;

        [SyncVar(hook = nameof(syncCooldownScales))]
        SkillSlotCooldownScalesWrapper _cooldownScales;

        public float[] CooldownScales
        {
            get => _cooldownScales;
            set => _cooldownScales = value;
        }

        [SyncVar(hook = nameof(syncStockAdds))]
        SkillSlotStockAddsWrapper _stockAdds;

        public int[] StockAdds
        {
            get => _stockAdds;
            set => _stockAdds = value;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            refreshAllSkills();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            float[] skillSlotCooldownScales = new float[SkillSlotModificationManager.SKILL_SLOT_COUNT];
            ArrayUtils.SetAll(skillSlotCooldownScales, 1f);

            CooldownScales = skillSlotCooldownScales;

            int[] skillSlotStockAdds = new int[SkillSlotModificationManager.SKILL_SLOT_COUNT];
            StockAdds = skillSlotStockAdds;
        }

        void OnEnable()
        {
            refreshAllSkills();
        }

        void OnDisable()
        {
            refreshAllSkills();
        }

        void syncLockedSkillSlotsMask(uint newLockedSkillSlotsMask)
        {
            // Creates mask where 1's mark any change in the locked skills mask
            uint changedSlotsMask = LockedSkillSlotsMask ^ newLockedSkillSlotsMask;

            LockedSkillSlotsMask = newLockedSkillSlotsMask;

            for (SkillSlot i = 0; i < (SkillSlot)SkillSlotModificationManager.SKILL_SLOT_COUNT; i++)
            {
                if (SkillSlotModificationManager.IsSkillSlotBitSet(changedSlotsMask, i)) // If this slot was changed
                {
                    if (SkillSlotModificationManager.IsSkillSlotBitSet(newLockedSkillSlotsMask, i))
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

        void syncCooldownScales(SkillSlotCooldownScalesWrapper newCooldownScales)
        {
            _cooldownScales = newCooldownScales;

            refreshAllSkills();
        }

        void syncStockAdds(SkillSlotStockAddsWrapper newStockAdds)
        {
            _stockAdds = newStockAdds;

            refreshAllSkills();
        }

        void refreshAllSkills()
        {
            foreach (GenericSkillTracker skillTracker in InstanceTracker.GetInstancesList<GenericSkillTracker>())
            {
                if (skillTracker.Skill)
                {
                    skillTracker.Skill.RecalculateValues();
                }
            }
        }
    }
}
