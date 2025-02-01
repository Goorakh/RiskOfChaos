using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.EffectComponents.SubtitleProviders;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
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

        static readonly SpawnPool<BuffDef> _availableBuffs = new SpawnPool<BuffDef>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.BuffExpansionsProvider
        };

        [SystemInitializer(typeof(BuffCatalog), typeof(DotController), typeof(ExpansionUtils))]
        static void InitAvailableBuffs()
        {
            _availableBuffs.EnsureCapacity(BuffCatalog.buffCount);

            _availableBuffs.CalcIsEntryAvailable += ApplyBuffEffect.CanSelectBuff;

            const float ELITE_WEIGHT = 0.8f;

            _availableBuffs.AddEntry(RoR2Content.Buffs.AffixRed, new SpawnPoolEntryParameters(ELITE_WEIGHT)); // EliteFire
            _availableBuffs.AddEntry(RoR2Content.Buffs.AffixHaunted, new SpawnPoolEntryParameters(ELITE_WEIGHT)); // EliteHaunted
            _availableBuffs.AddEntry(RoR2Content.Buffs.AffixWhite, new SpawnPoolEntryParameters(ELITE_WEIGHT)); // EliteIce
            _availableBuffs.AddEntry(RoR2Content.Buffs.AffixBlue, new SpawnPoolEntryParameters(ELITE_WEIGHT)); // EliteLightning
            _availableBuffs.AddEntry(RoR2Content.Buffs.AffixLunar, new SpawnPoolEntryParameters(ELITE_WEIGHT)); // EliteLunar
            _availableBuffs.AddEntry(RoR2Content.Buffs.AffixPoison, new SpawnPoolEntryParameters(ELITE_WEIGHT)); // ElitePoison
            _availableBuffs.AddEntry(RoR2Content.Buffs.ArmorBoost, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.AttackSpeedOnCrit, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.BanditSkull, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.BugWings, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.CloakSpeed, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.CrocoRegen, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.ElephantArmorBoost, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.Energized, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.EngiShield, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.FullCrit, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.LifeSteal, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.LunarShell, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.NoCooldowns, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.PowerBuff, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.SmallArmorBoost, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.TeamWarCry, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.TonicBuff, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.Warbanner, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.WarCryBuff, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(RoR2Content.Buffs.WhipBoost, new SpawnPoolEntryParameters(1f));
            
            _availableBuffs.AddEntry(JunkContent.Buffs.EngiTeamShield, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(JunkContent.Buffs.EnrageAncientWisp, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(JunkContent.Buffs.LoaderPylonPowered, new SpawnPoolEntryParameters(1f));
            _availableBuffs.AddEntry(JunkContent.Buffs.MeatRegenBoost, new SpawnPoolEntryParameters(1f));

            _availableBuffs.AddEntry(DLC1Content.Buffs.EliteEarth, new SpawnPoolEntryParameters(ELITE_WEIGHT, ExpansionUtils.DLC1));
            _availableBuffs.AddEntry(DLC1Content.Buffs.EliteVoid, new SpawnPoolEntryParameters(ELITE_WEIGHT, ExpansionUtils.DLC1));
            _availableBuffs.AddEntry(DLC1Content.Buffs.KillMoveSpeed, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));
            _availableBuffs.AddEntry(DLC1Content.Buffs.VoidSurvivorCorruptMode, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));

            _availableBuffs.AddEntry(DLC2Content.Buffs.EliteAurelionite, new SpawnPoolEntryParameters(ELITE_WEIGHT, ExpansionUtils.DLC2));
            _availableBuffs.AddEntry(DLC2Content.Buffs.ElusiveAntlersBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            _availableBuffs.AddEntry(DLC2Content.Buffs.HealAndReviveRegenBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            _availableBuffs.AddEntry(DLC2Content.Buffs.IncreaseDamageBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            _availableBuffs.AddEntry(DLC2Content.Buffs.IncreasePrimaryDamageBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
            _availableBuffs.AddEntry(DLC2Content.Buffs.LowerHealthHigherDamageBuff, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));

            _availableBuffs.TrimExcess();

#if DEBUG
            for (int i = 0; i < BuffCatalog.buffCount; i++)
            {
                BuffIndex buffIndex = (BuffIndex)i;
                BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
                if (!buffDef || buffDef.isHidden || buffDef.isCooldown || DotController.GetDotDefIndex(buffDef) != DotController.DotIndex.None)
                    continue;

                if (!_availableBuffs.Contains(buffDef))
                {
                    Log.Debug($"Not including {buffDef.name} as buff");
                }
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

            BuffDef buff = _availableBuffs.PickRandomEntry(rng);
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
        }
    }
}
