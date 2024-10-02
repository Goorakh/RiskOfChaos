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
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("force_activate_random_skill", 90f, DefaultSelectionWeight = 0.6f)]
    [EffectConfigBackwardsCompatibility("Effect: Force Activate Random Skill (Lasts 1 stage)")]
    public sealed class ForceActivateRandomSkill : TimedEffect, ISkillSlotModificationProvider
    {
        static ConfigHolder<bool> createSkillAllowedConfig(SkillSlot slot)
        {
            return ConfigFactory<bool>.CreateConfig($"Force {slot}", true)
                                      .Description($"If {slot} skills should be allowed to be forced")
                                      .OptionConfig(new CheckBoxConfig())
                                      .Build();
        }

        [EffectConfig] static readonly ConfigHolder<bool> _allowForcePrimary = createSkillAllowedConfig(SkillSlot.Primary);
        [EffectConfig] static readonly ConfigHolder<bool> _allowForceSecondary = createSkillAllowedConfig(SkillSlot.Secondary);
        [EffectConfig] static readonly ConfigHolder<bool> _allowForceUtility = createSkillAllowedConfig(SkillSlot.Utility);
        [EffectConfig] static readonly ConfigHolder<bool> _allowForceSpecial = createSkillAllowedConfig(SkillSlot.Special);

        static bool canForceSkill(SkillSlot slot)
        {
            return slot switch
            {
                SkillSlot.Primary => _allowForcePrimary.Value,
                SkillSlot.Secondary => _allowForceSecondary.Value,
                SkillSlot.Utility => _allowForceUtility.Value,
                SkillSlot.Special => _allowForceSpecial.Value,
                _ => true,
            };
        }

        static IEnumerable<SkillSlot> getAllForcableSkillSlots()
        {
            uint lockedSlotsMask = SkillSlotModificationManager.Instance.LockedSkillSlotsMask;
            uint forceActivatesSlotsMask = SkillSlotModificationManager.Instance.ForceActivateSkillSlotsMask;

            uint nonLockedNonForceActivatedSlotsMask = ~(lockedSlotsMask | forceActivatesSlotsMask);

            for (SkillSlot i = 0; i < (SkillSlot)SkillSlotModificationManager.SKILL_SLOT_COUNT; i++)
            {
                if (SkillSlotModificationManager.IsSkillSlotBitSet(nonLockedNonForceActivatedSlotsMask, i) && canForceSkill(i))
                {
                    yield return i;
                }
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance && getAllForcableSkillSlots().Any();
        }

        SkillSlot _forcedSkillSlot = SkillSlot.None;

        public event Action OnValueDirty;

        public void ModifyValue(ref SkillSlotModificationData modification)
        {
            if (modification.SlotIndex == _forcedSkillSlot)
            {
                modification.ForceActivate = true;
            }
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _forcedSkillSlot = RNG.NextElementUniform(getAllForcableSkillSlots().ToArray());
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write((sbyte)_forcedSkillSlot);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _forcedSkillSlot = (SkillSlot)reader.ReadSByte();
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
