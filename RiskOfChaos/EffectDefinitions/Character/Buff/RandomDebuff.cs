using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.CatalogIndexCollection;
using RoR2;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosTimedEffect("random_debuff", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.9f, EffectWeightReductionPercentagePerActivation = 15f)]
    [EffectConfigBackwardsCompatibility("Effect: Give Everyone a Random Debuff (Lasts 1 stage)")]
    public sealed class RandomDebuff : ApplyBuffEffect
    {
        static readonly BuffIndexCollection _debuffBlacklist = new BuffIndexCollection(new string[]
        {
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
        });

        static BuffIndex[] _availableBuffIndices;

        [SystemInitializer(typeof(BuffCatalog), typeof(DotController))]
        static void InitAvailableBuffs()
        {
            _availableBuffIndices = Enumerable.Range(0, BuffCatalog.buffCount).Select(i => (BuffIndex)i).Where(bi =>
            {
                if (bi == BuffIndex.None)
                    return false;

                BuffDef buffDef = BuffCatalog.GetBuffDef(bi);
                if (!buffDef || buffDef.isHidden || !isDebuff(buffDef) || isCooldown(buffDef))
                {
#if DEBUG
                    Log.Debug($"Excluding hidden/buff/cooldown: {buffDef.name}");
#endif
                    return false;
                }

                if (isDOT(buffDef))
                {
#if DEBUG
                    Log.Debug($"Excluding DOT buff: {buffDef.name}");
#endif
                    return false;
                }

                if (_debuffBlacklist.Contains(buffDef.buffIndex))
                {
#if DEBUG
                    Log.Debug($"Excluding debuff {buffDef.name}: blacklist");
#endif
                    return false;
                }

#if DEBUG
                Log.Debug($"Including debuff {buffDef.name}");
#endif

                return true;
            }).ToArray();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _availableBuffIndices != null && filterSelectableBuffs(_availableBuffIndices).Any();
        }

#if DEBUG
        static int _debugIndex = 0;
        static bool _enableDebugIndex = false;
#endif

        protected override BuffIndex getBuffIndexToApply()
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
                selectedBuff = RNG.NextElementUniform(filterSelectableBuffs(_availableBuffIndices).ToList());
            }

#if DEBUG
            BuffDef buffDef = BuffCatalog.GetBuffDef(selectedBuff);
            Log.Debug($"Applying buff {buffDef?.name ?? "null"}");
#endif

            return selectedBuff;
        }
    }
}
