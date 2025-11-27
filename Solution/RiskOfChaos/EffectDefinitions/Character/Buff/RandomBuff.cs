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
    [ChaosTimedEffect("random_buff", 90f)]
    [EffectConfigBackwardsCompatibility("Effect: Give Everyone a Random Buff (Lasts 1 stage)")]
    [RequiredComponents(typeof(ApplyBuffEffect), typeof(BuffSubtitleProvider))]
    public sealed class RandomBuff : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _stackableBuffCount =
            ConfigFactory<int>.CreateConfig("Buff Stack Count", 5)
                              .Description("How many stacks of the buff should be given, if the random buff is stackable")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        static readonly SpawnPool<BuffDef> _availableBuffs = new SpawnPool<BuffDef>();

        [SystemInitializer(typeof(BuffCatalog), typeof(DotController), typeof(ExpansionUtils))]
        static void InitAvailableBuffs()
        {
            _availableBuffs.EnsureCapacity(BuffCatalog.buffCount);

            const float ELITE_WEIGHT = 0.8f;

            void addBuffEntry(BuffDef buff, SpawnPoolEntryParameters parameters)
            {
                float weightMultiplier = 1f;
                if (buff.isElite)
                {
                    weightMultiplier *= ELITE_WEIGHT;
                }

                parameters.Weight *= weightMultiplier;

                BuffIndex buffIndex = buff.buffIndex;
                parameters.IsAvailableFunc = () => ApplyBuffEffect.CanSelectBuff(buffIndex);
                _availableBuffs.AddEntry(buff, parameters);
            }

            addBuffEntry(RoR2Content.Buffs.AffixRed, new SpawnPoolEntryParameters(1f)); // EliteFire
            addBuffEntry(RoR2Content.Buffs.AffixHaunted, new SpawnPoolEntryParameters(1f)); // EliteHaunted
            addBuffEntry(RoR2Content.Buffs.AffixWhite, new SpawnPoolEntryParameters(1f)); // EliteIce
            addBuffEntry(RoR2Content.Buffs.AffixBlue, new SpawnPoolEntryParameters(1f)); // EliteLightning
            addBuffEntry(RoR2Content.Buffs.AffixLunar, new SpawnPoolEntryParameters(1f)); // EliteLunar
            addBuffEntry(RoR2Content.Buffs.AffixPoison, new SpawnPoolEntryParameters(1f)); // ElitePoison
            addBuffEntry(RoR2Content.Buffs.ArmorBoost, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.AttackSpeedOnCrit, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.BanditSkull, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.BugWings, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.CloakSpeed, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.CrocoRegen, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.ElephantArmorBoost, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.Energized, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.EngiShield, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.FullCrit, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.LifeSteal, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.LunarShell, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.NoCooldowns, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.PowerBuff, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.SmallArmorBoost, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.TeamWarCry, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.TonicBuff, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.Warbanner, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.WarCryBuff, new SpawnPoolEntryParameters(1f));
            addBuffEntry(RoR2Content.Buffs.WhipBoost, new SpawnPoolEntryParameters(1f));
            
            addBuffEntry(JunkContent.Buffs.EngiTeamShield, new SpawnPoolEntryParameters(1f));
            addBuffEntry(JunkContent.Buffs.EnrageAncientWisp, new SpawnPoolEntryParameters(1f));
            addBuffEntry(JunkContent.Buffs.LoaderPylonPowered, new SpawnPoolEntryParameters(1f));
            addBuffEntry(JunkContent.Buffs.MeatRegenBoost, new SpawnPoolEntryParameters(1f));

            addBuffEntry(DLC1Content.Buffs.EliteEarth, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));
            addBuffEntry(DLC1Content.Buffs.EliteVoid, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));
            addBuffEntry(DLC1Content.Buffs.KillMoveSpeed, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));
            addBuffEntry(DLC1Content.Buffs.VoidSurvivorCorruptMode, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));

            addBuffEntry(DLC2Content.Buffs.EliteAurelionite, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.ElusiveAntlersBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.HealAndReviveRegenBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.IncreaseDamageBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.IncreasePrimaryDamageBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            addBuffEntry(DLC2Content.Buffs.AttackSpeedPerNearbyAllyOrEnemyBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));

            addBuffEntry(DLC3Content.Buffs.StimShot, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.DroneArmor, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.CollectiveShareBuff, new SpawnPoolEntryParameters(ELITE_WEIGHT, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.PowerCubeBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.PowerPyramidBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.JumpDamageStrikeCharge, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.SpeedOnPickup, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.ShockDamageEnergized, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.CritChanceAndDamage, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));
            addBuffEntry(DLC3Content.Buffs.HealNovaRegen, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC3));

            _availableBuffs.TrimExcess();

#if DEBUG
            AssetOrDirectReference<BuffDef>[] availableBuffReferences = [.. _availableBuffs];

            for (int i = 0; i < BuffCatalog.buffCount; i++)
            {
                BuffIndex buffIndex = (BuffIndex)i;
                BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
                if (!buffDef || buffDef.isHidden || buffDef.isCooldown || DotController.GetDotDefIndex(buffDef) != DotController.DotIndex.None)
                    continue;

                if (!availableBuffReferences.Any(r => r.WaitForAsset() == buffDef))
                {
                    Log.Debug($"Not including {buffDef.name} as buff");
                }
            }

            foreach (AssetOrDirectReference<BuffDef> buffRef in availableBuffReferences)
            {
                buffRef.Reset();
            }
#endif
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _availableBuffs.AnyAvailable;
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

            AssetOrDirectReference<BuffDef> buffRef = _availableBuffs.PickRandomEntry(rng);
            BuffDef buff = buffRef.WaitForAsset();
            if (buff)
            {
                Log.Debug($"Applying buff {buff}");

                _applyBuffEffect.BuffIndex = buff.buffIndex;
                _applyBuffEffect.BuffStackCount = buff.canStack ? _stackableBuffCount.Value : 1;
            }
            else
            {
                Log.Error("No buff selected");
            }

            buffRef.Reset();
        }
    }
}
