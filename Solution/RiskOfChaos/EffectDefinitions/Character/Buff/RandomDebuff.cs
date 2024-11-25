using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosTimedEffect("random_debuff", 60f)]
    [EffectConfigBackwardsCompatibility("Effect: Give Everyone a Random Debuff (Lasts 1 stage)")]
    [RequiredComponents(typeof(ApplyBuffEffect))]
    public sealed class RandomDebuff : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _stackableDebuffCount =
            ConfigFactory<int>.CreateConfig("Debuff Stack Count", 10)
                              .Description("How many stacks of the debuff should be given, if the random debuff is stackable")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        static readonly BuffIndexCollection _debuffBlacklist = new BuffIndexCollection([
            "bdDisableAllSkills", // Nullref spam and not fun
            "bdEliteBeadThorns", // Does nothing
            "bdEntangle", // Immobile
            "bdLunarSecondaryRoot", // Immobile
            "bdNullified", // Immobile
            "bdNullifyStack", // Does nothing
            "bdOverheat", // Does nothing
            "bdPulverizeBuildup", // Does nothing

            #region VanillaVoid compat
            "ZnVVlotusSlow", // Doesn't work without item
            #endregion

            #region MysticsItems compat
            "MysticsItems_Crystallized", // Immobile
            #endregion

            #region Starstorm2 compat
            "bdMULENet", // Basically immobile
            "bdPurplePoison", // Does nothing
            "BuffNeedleBuildup", // Doesn't work without item
            #endregion
        ]);

        static BuffIndex[] _availableBuffIndices = [];

        [SystemInitializer(typeof(BuffCatalog), typeof(DotController))]
        static void InitAvailableBuffs()
        {
            _availableBuffIndices = Enumerable.Range(0, BuffCatalog.buffCount).Select(i => (BuffIndex)i).Where(bi =>
            {
                if (bi == BuffIndex.None)
                    return false;

                BuffDef buffDef = BuffCatalog.GetBuffDef(bi);
                if (!buffDef || buffDef.isHidden || !BuffUtils.IsDebuff(buffDef) || BuffUtils.IsCooldown(buffDef))
                {
                    Log.Debug($"Excluding hidden/buff/cooldown: {buffDef.name}");
                    return false;
                }

                if (BuffUtils.IsDOT(buffDef))
                {
                    Log.Debug($"Excluding DOT buff: {buffDef.name}");
                    return false;
                }

                if (_debuffBlacklist.Contains(buffDef.buffIndex))
                {
                    Log.Debug($"Excluding debuff {buffDef.name}: blacklist");
                    return false;
                }

                Log.Debug($"Including debuff {buffDef.name}");

                return true;
            }).ToArray();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _availableBuffIndices.Length > 0 && ApplyBuffEffect.FilterSelectableBuffs(_availableBuffIndices).Any();
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

            BuffIndex buffIndex = getBuffIndexToApply(rng);
            _applyBuffEffect.BuffIndex = buffIndex;

            BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
            _applyBuffEffect.BuffStackCount = buffDef && buffDef.canStack ? _stackableDebuffCount.Value : 1;
        }

#if DEBUG
        static int _debugIndex = 0;
        static bool _enableDebugIndex = false;
#endif

        static BuffIndex getBuffIndexToApply(Xoroshiro128Plus rng)
        {
            BuffIndex selectedBuff;

#if DEBUG
            if (_enableDebugIndex)
            {
                selectedBuff = _availableBuffIndices[_debugIndex++ % _availableBuffIndices.Length];
            }
            else
#endif
            {
                selectedBuff = rng.NextElementUniform(ApplyBuffEffect.FilterSelectableBuffs(_availableBuffIndices).ToList());
            }

            Log.Debug($"Applying debuff {BuffCatalog.GetBuffDef(selectedBuff)}");

            return selectedBuff;
        }
    }
}
