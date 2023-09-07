using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.SkillSlots;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosTimedEffect("lock_random_skill", 90f, DefaultSelectionWeight = 0.5f)]
    [EffectConfigBackwardsCompatibility("Effect: Disable Random Skill (Lasts 1 stage)")]
    public sealed class LockRandomSkill : TimedEffect, ISkillSlotModificationProvider
    {
        static ConfigHolder<bool> createSkillAllowedConfig(SkillSlot slot)
        {
            return ConfigFactory<bool>.CreateConfig($"Disable {slot}", true)
                                      .Description($"If {slot} skills should be allowed to be disabled")
                                      .OptionConfig(new CheckBoxConfig())
                                      .Build();
        }

        [EffectConfig] static readonly ConfigHolder<bool> _allowLockPrimary = createSkillAllowedConfig(SkillSlot.Primary);
        [EffectConfig] static readonly ConfigHolder<bool> _allowLockSecondary = createSkillAllowedConfig(SkillSlot.Secondary);
        [EffectConfig] static readonly ConfigHolder<bool> _allowLockUtility = createSkillAllowedConfig(SkillSlot.Utility);
        [EffectConfig] static readonly ConfigHolder<bool> _allowLockSpecial = createSkillAllowedConfig(SkillSlot.Special);

        static bool canLockSkill(SkillSlot slot)
        {
            return slot switch
            {
                SkillSlot.Primary => _allowLockPrimary.Value,
                SkillSlot.Secondary => _allowLockSecondary.Value,
                SkillSlot.Utility => _allowLockUtility.Value,
                SkillSlot.Special => _allowLockSpecial.Value,
                _ => true,
            };
        }

        static IEnumerable<SkillSlot> getAllLockableSkillSlots()
        {
            return SkillSlotModificationManager.Instance.NonLockedSkillSlots.Where(canLockSkill);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance && getAllLockableSkillSlots().Any();
        }

        SkillSlot _lockedSkillSlot = SkillSlot.None;

        public event Action OnValueDirty;

        public void ModifyValue(ref SkillSlotModificationData modification)
        {
            if (modification.SlotIndex == _lockedSkillSlot)
            {
                modification.ForceIsLocked = true;
            }
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _lockedSkillSlot = RNG.NextElementUniform(getAllLockableSkillSlots().ToList());
        }

        public override void OnStart()
        {
            SkillSlotModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (SkillSlotModificationManager.Instance)
            {
                SkillSlotModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
