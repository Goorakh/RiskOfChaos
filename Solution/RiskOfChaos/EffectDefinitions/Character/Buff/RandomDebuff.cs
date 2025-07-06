using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ContentManagement;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosTimedEffect("random_debuff", 60f)]
    [EffectConfigBackwardsCompatibility("Effect: Give Everyone a Random Debuff (Lasts 1 stage)")]
    [RequiredComponents(typeof(ApplyBuffEffect), typeof(BuffSubtitleProvider))]
    public sealed class RandomDebuff : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _stackableDebuffCount =
            ConfigFactory<int>.CreateConfig("Debuff Stack Count", 10)
                              .Description("How many stacks of the debuff should be given, if the random debuff is stackable")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        static readonly SpawnPool<BuffDef> _availableDebuffs = new SpawnPool<BuffDef>();

        [SystemInitializer(typeof(BuffCatalog), typeof(DotController), typeof(ExpansionUtils))]
        static void InitAvailableBuffs()
        {
            _availableDebuffs.EnsureCapacity(BuffCatalog.buffCount);

            static void addBuffEntry(BuffDef buff, SpawnPoolEntryParameters parameters)
            {
                _availableDebuffs.AddEntry(getBuffEntry(buff, parameters));
            }

            static SpawnPoolEntry<BuffDef> getBuffEntry(BuffDef buff, SpawnPoolEntryParameters parameters)
            {
                BuffIndex buffIndex = buff.buffIndex;
                parameters.IsAvailableFunc = () => ApplyBuffEffect.CanSelectBuff(buffIndex);
                return _availableDebuffs.CreateEntry(buff, parameters);
            }

            addBuffEntry(RoR2Content.Buffs.BeetleJuice, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.ClayGoo, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.Cripple, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.DeathMark, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.Fruiting, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.HealingDisabled, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.LunarDetonationCharge, new SpawnPoolEntryParameters(0.7f));
            addBuffEntry(RoR2Content.Buffs.MercExpose, new SpawnPoolEntryParameters(0.7f));
            addBuffEntry(RoR2Content.Buffs.PermanentCurse, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.Pulverized, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.Slow50, new SpawnPoolEntryParameters(0.8f));
            addBuffEntry(RoR2Content.Buffs.Slow60, new SpawnPoolEntryParameters(0.8f));
            addBuffEntry(RoR2Content.Buffs.Slow80, new SpawnPoolEntryParameters(0.8f));
            addBuffEntry(RoR2Content.Buffs.Weak, new SpawnPoolEntryParameters(1f));

            addBuffEntry(JunkContent.Buffs.Slow30, new SpawnPoolEntryParameters(1f));

            addBuffEntry(DLC1Content.Buffs.Blinded, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));
            addBuffEntry(DLC1Content.Buffs.JailerSlow, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC1));
            addBuffEntry(DLC1Content.Buffs.PermanentDebuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));

            _availableDebuffs.AddGroupedEntries([
                getBuffEntry(DLC2Content.Buffs.CookingChopped, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
                getBuffEntry(DLC2Content.Buffs.CookingOiled, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
                getBuffEntry(DLC2Content.Buffs.CookingRoasted, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
                getBuffEntry(DLC2Content.Buffs.CookingRolled, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
                getBuffEntry(DLC2Content.Buffs.CookingChilled, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2)),
            ], 0.7f);

            addBuffEntry(DLC2Content.Buffs.KnockUpHitEnemies, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.SoulCost, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.Oiled, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.SeekerRevivedOnce, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.Frost, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC2));

            _availableDebuffs.TrimExcess();

#if DEBUG
            AssetOrDirectReference<BuffDef>[] availableDebuffReferences = [.. _availableDebuffs];

            for (int i = 0; i < BuffCatalog.buffCount; i++)
            {
                BuffIndex buffIndex = (BuffIndex)i;
                BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
                if (!buffDef || buffDef.isHidden || buffDef.isCooldown || DotController.GetDotDefIndex(buffDef) != DotController.DotIndex.None)
                    continue;

                if (!availableDebuffReferences.Any(r => r.WaitForAsset() == buffDef))
                {
                    Log.Debug($"Not including {buffDef.name} as debuff");
                }
            }

            foreach (AssetOrDirectReference<BuffDef> debuffRef in availableDebuffReferences)
            {
                debuffRef.Reset();
            }
#endif
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _availableDebuffs.AnyAvailable;
        }

        ChaosEffectComponent _chaosEffect;
        ApplyBuffEffect _applyBuffEffect;

        void Awake()
        {
            _chaosEffect = GetComponent<ChaosEffectComponent>();
            _applyBuffEffect = GetComponent<ApplyBuffEffect>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_chaosEffect.Rng.nextUlong);

            AssetOrDirectReference<BuffDef> buffRef = _availableDebuffs.PickRandomEntry(rng);
            BuffDef buff = buffRef.WaitForAsset();
            if (buff)
            {
                Log.Debug($"Applying debuff {buff}");

                _applyBuffEffect.BuffIndex = buff.buffIndex;
                _applyBuffEffect.BuffStackCount = buff.canStack ? _stackableDebuffCount.Value : 1;
            }
            else
            {
                Log.Error("No debuff selected");
            }

            buffRef.Reset();
        }
    }
}
