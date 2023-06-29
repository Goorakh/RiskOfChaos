using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosEffect("random_debuff", DefaultSelectionWeight = 0.9f, EffectWeightReductionPercentagePerActivation = 15f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: Give Everyone a Random Debuff (Lasts 1 stage)")]
    public sealed class RandomDebuff : ApplyBuffEffect
    {
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

                switch (buffDef.name)
                {
                    case "bdEntangle": // Immobile
                    case "bdLunarSecondaryRoot": // Immobile
                    case "bdNullified": // Immobile
                    case "bdNullifyStack": // Does nothing
                    case "bdOverheat": // Does nothing
                    case "bdPulverizeBuildup": // Does nothing
#if DEBUG
                        Log.Debug($"Excluding debuff {buffDef.name}: blacklist");
#endif
                        return false;

                    #region VanillaVoid compat
                    case "ZnVVlotusSlow": // Doesn't work without item
#if DEBUG
                        Log.Debug($"Excluding debuff {buffDef.name}: VanillaVoid compat blacklist");
#endif
                        return false;
                    #endregion

                    #region MysticsItems compat
                    case "MysticsItems_Crystallized": // Immobile
#if DEBUG
                        Log.Debug($"Excluding debuff {buffDef.name}: MysticsItems compat blacklist");
#endif
                        return false;
                    #endregion

                    #region Starstorm2 compat
                    case "bdMULENet": // Basically immobile
                    case "bdPurplePoison": // Does nothing
                    case "BuffNeedleBuildup": // Doesn't work without item
#if DEBUG
                        Log.Debug($"Excluding debuff {buffDef.name}: Starstorm2 compat blacklist");
#endif
                        return false;
                    #endregion
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
