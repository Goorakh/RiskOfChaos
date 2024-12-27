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
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("lock_random_skill", 90f, DefaultSelectionWeight = 0.7f)]
    [RequiredComponents(typeof(SkillSlotSubtitleProvider))]
    [EffectConfigBackwardsCompatibility("Effect: Disable Random Skill (Lasts 1 stage)")]
    public sealed class LockRandomSkill : NetworkBehaviour
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

        [EffectCanActivate]
        static bool CanActivate()
        {
            if (!RoCContent.NetworkedPrefabs.SkillSlotModificationProvider)
                return false;

            SkillSlotModificationManager skillSlotModificationManager = SkillSlotModificationManager.Instance;
            if (!skillSlotModificationManager)
                return false;

            SkillSlotMask nonLockedSlotsMask = ~skillSlotModificationManager.LockedSlots;

            return nonLockedSlotsMask.ContainedSlotCount > 0;
        }

        static List<SkillSlot> getAllLockableSkillSlots()
        {
            SkillSlotMask nonLockedSlots = ~SkillSlotModificationManager.Instance.LockedSlots;

            List<SkillSlot> lockableSkillSlots = new List<SkillSlot>(nonLockedSlots.ContainedSlotCount);

            foreach (SkillSlot nonLockedSlot in nonLockedSlots)
            {
                if (canLockSkill(nonLockedSlot))
                {
                    lockableSkillSlots.Add(nonLockedSlot);
                }
            }

            return lockableSkillSlots;
        }

        ChaosEffectComponent _effectComponent;
        SkillSlotSubtitleProvider _skillSlotSubtitleProvider;

        [SerializedMember("s")]
        SkillSlot _lockedSkillSlot = SkillSlot.None;

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

            List<SkillSlot> lockableSkillSlots = getAllLockableSkillSlots();
            if (lockableSkillSlots.Count > 0)
            {
                _lockedSkillSlot = rng.NextElementUniform(lockableSkillSlots);
            }
            else
            {
                Log.Error("No available skill slots");
                _lockedSkillSlot = SkillSlot.None;
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _skillSlotModificationController = Instantiate(RoCContent.NetworkedPrefabs.SkillSlotModificationProvider).GetComponent<ValueModificationController>();

                SkillSlotModificationProvider skillSlotModificationProvider = _skillSlotModificationController.GetComponent<SkillSlotModificationProvider>();
                skillSlotModificationProvider.LockedSlots = _lockedSkillSlot;

                NetworkServer.Spawn(_skillSlotModificationController.gameObject);

                if (_skillSlotSubtitleProvider)
                {
                    _skillSlotSubtitleProvider.SkillSlot = _lockedSkillSlot;
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
