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
                if (!buffDef || buffDef.isHidden || !isDebuff(buffDef) || buffDef.isCooldown)
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

        static int _debugIndex = 0;

        protected override BuffIndex getBuffIndexToApply()
        {
            BuffIndex selectedBuff = RNG.NextElementUniform(filterSelectableBuffs(_availableBuffIndices).ToList());
            // selectedBuff = _availableBuffIndices[_debugIndex++ % _availableBuffIndices.Length];

#if DEBUG
            BuffDef buffDef = BuffCatalog.GetBuffDef(selectedBuff);
            Log.Debug($"Applying buff {buffDef?.name ?? "null"}");
#endif

            return selectedBuff;
        }
    }
}
