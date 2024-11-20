using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("lock_random_skill", 90f, DefaultSelectionWeight = 0.5f)]
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

            List<SkillSlot> lockableSkillSlots = new List<SkillSlot>(SkillSlotUtils.SkillSlotCount);

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
        ChaosEffectNameComponent _effectNameComponent;

        [SerializedMember("s")]
        SkillSlot _lockedSkillSlot = SkillSlot.None;

        ValueModificationController _skillSlotModificationController;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectNameComponent = GetComponent<ChaosEffectNameComponent>();
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

                if (_effectNameComponent)
                {
                    _effectNameComponent.SetCustomNameFormatter(new NameFormatter(_lockedSkillSlot));
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

        class NameFormatter : EffectNameFormatter
        {
            SkillSlot _skillSlot = SkillSlot.None;

            public NameFormatter(SkillSlot skillSlot)
            {
                _skillSlot = skillSlot;
            }

            public NameFormatter()
            {
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write((sbyte)_skillSlot);
            }

            public override void Deserialize(NetworkReader reader)
            {
                _skillSlot = (SkillSlot)reader.ReadSByte();
                invokeFormatterDirty();
            }

            public override string GetEffectNameSubtitle(ChaosEffectInfo effectInfo)
            {
                string subtitle = base.GetEffectNameSubtitle(effectInfo);

                if (_skillSlot > SkillSlot.None && _skillSlot <= SkillSlot.Special)
                {
                    StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

                    if (!string.IsNullOrWhiteSpace(subtitle))
                    {
                        stringBuilder.AppendLine(subtitle);
                    }

                    stringBuilder.AppendFormat("({0:G})", _skillSlot);

                    subtitle = stringBuilder.ToString();
                    stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
                }

                return subtitle;
            }

            public override object[] GetFormatArgs()
            {
                return [];
            }

            public override bool Equals(EffectNameFormatter other)
            {
                return other is NameFormatter otherFormatter &&
                       _skillSlot == otherFormatter._skillSlot;
            }
        }
    }
}
