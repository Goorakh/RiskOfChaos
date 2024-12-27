using RiskOfChaos.ModificationController.SkillSlots;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class GenericSkillTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GenericSkill.Awake += (orig, self) =>
            {
                orig(self);

                GenericSkillTracker tracker = self.gameObject.AddComponent<GenericSkillTracker>();
                tracker.Skill = self;
            };
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
