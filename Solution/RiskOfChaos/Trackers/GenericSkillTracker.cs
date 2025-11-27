using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class GenericSkillTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            GenericSkillHooks.OnGenericSkillAwakeGlobal += onGenericSkillAwakeGlobal;
        }

        static void onGenericSkillAwakeGlobal(GenericSkill skill)
        {
            GenericSkillTracker tracker = skill.gameObject.AddComponent<GenericSkillTracker>();
            tracker.Skill = skill;
        }

        public GenericSkill Skill { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);

            SkillSlotModificationManager.OnCooldownMultiplierChanged += onGlobalCooldownMultiplierChanged;
            SkillSlotModificationManager.OnStockAddChanged += onGlobalStockAddChanged;
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);

            SkillSlotModificationManager.OnCooldownMultiplierChanged -= onGlobalCooldownMultiplierChanged;
            SkillSlotModificationManager.OnStockAddChanged -= onGlobalStockAddChanged;
        }

        void onGlobalCooldownMultiplierChanged()
        {
            if (Skill)
            {
                Skill.RecalculateFinalRechargeInterval();
            }
        }

        void onGlobalStockAddChanged()
        {
            if (Skill)
            {
                Skill.RecalculateMaxStock();
            }
        }
    }
}
