using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.SaveHandling;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Skill
{
    [ChaosTimedEffect("force_activate_random_skill", 90f, DefaultSelectionWeight = 0.6f)]
    [EffectConfigBackwardsCompatibility("Effect: Force Activate Random Skill (Lasts 1 stage)")]
    [RequiredComponents(typeof(SkillSlotSubtitleProvider))]
    public sealed class ForceActivateRandomSkill : NetworkBehaviour
    {
        static ConfigHolder<bool> createSkillAllowedConfig(SkillSlot slot)
        {
            return ConfigFactory<bool>.CreateConfig($"Force {slot}", true)
                                      .Description($"If {slot} skills should be allowed to be forced")
                                      .OptionConfig(new CheckBoxConfig())
                                      .Build();
        }

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowForcePrimary = createSkillAllowedConfig(SkillSlot.Primary);
        
        [EffectConfig]
        static readonly ConfigHolder<bool> _allowForceSecondary = createSkillAllowedConfig(SkillSlot.Secondary);

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowForceUtility = createSkillAllowedConfig(SkillSlot.Utility);

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowForceSpecial = createSkillAllowedConfig(SkillSlot.Special);

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

        static List<SkillSlot> getAllForcableSkillSlots()
        {
            SkillSlotMask lockedSlotsMask = SkillSlotModificationManager.Instance.LockedSlots;
            SkillSlotMask forceActivatesSlotsMask = SkillSlotModificationManager.Instance.ForceActivatedSlots;

            SkillSlotMask nonLockedNonForceActivatedSlotsMask = ~(lockedSlotsMask | forceActivatesSlotsMask);

            List<SkillSlot> forcableSkillSlots = new List<SkillSlot>(nonLockedNonForceActivatedSlotsMask.ContainedSlotCount);
            foreach (SkillSlot availableSlot in nonLockedNonForceActivatedSlotsMask)
            {
                if (canForceSkill(availableSlot))
                {
                    forcableSkillSlots.Add(availableSlot);
                }
            }

            return forcableSkillSlots;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            if (!RoCContent.NetworkedPrefabs.SkillSlotModificationProvider)
                return false;

            SkillSlotModificationManager skillSlotModificationManager = SkillSlotModificationManager.Instance;
            if (!skillSlotModificationManager)
                return false;

            SkillSlotMask lockedSlotsMask = skillSlotModificationManager.LockedSlots;
            SkillSlotMask forceActivatesSlotsMask = skillSlotModificationManager.ForceActivatedSlots;

            SkillSlotMask nonLockedNonForceActivatedSlotsMask = ~(lockedSlotsMask | forceActivatesSlotsMask);

            return nonLockedNonForceActivatedSlotsMask.ContainedSlotCount > 0;
        }

        ChaosEffectComponent _effectComponent;
        SkillSlotSubtitleProvider _skillSlotSubtitleProvider;

        [SerializedMember("s")]
        SkillSlot _forcedSkillSlot = SkillSlot.None;

        ValueModificationController _skillSlotModificationController;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _skillSlotSubtitleProvider = GetComponent<SkillSlotSubtitleProvider>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            List<SkillSlot> allForcableSkillSlots = getAllForcableSkillSlots();
            if (allForcableSkillSlots.Count > 0)
            {
                _forcedSkillSlot = rng.NextElementUniform(allForcableSkillSlots);
            }
            else
            {
                _forcedSkillSlot = SkillSlot.None;
                Log.Error("No available skill slots");
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _skillSlotModificationController = Instantiate(RoCContent.NetworkedPrefabs.SkillSlotModificationProvider).GetComponent<ValueModificationController>();

                SkillSlotModificationProvider skillSlotModificationProvider = _skillSlotModificationController.GetComponent<SkillSlotModificationProvider>();
                skillSlotModificationProvider.ForceActivatedSlots = _forcedSkillSlot;

                NetworkServer.Spawn(_skillSlotModificationController.gameObject);

                if (_skillSlotSubtitleProvider)
                {
                    _skillSlotSubtitleProvider.SkillSlot = _forcedSkillSlot;
                }
            }
        }

        void OnDestroy()
        {
            if (_skillSlotModificationController)
            {
                _skillSlotModificationController.Retire();
                _skillSlotModificationController = null;
            }
        }
    }
}
