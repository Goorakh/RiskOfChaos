using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("increase_director_credits", EffectWeightReductionPercentagePerActivation = 35f)]
    public sealed class IncreaseDirectorCredits : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        const float CREDIT_MULTIPLIER = 1.5f;

        // Would've just used a simple component on the same object for this instead if it wasn't for multiple directors being on the same object in some cases
        static readonly Dictionary<CombatDirector, int> _directorAppliedCounts = new Dictionary<CombatDirector, int>();

        static bool _appliedPatch = false;
        static void applyPatchIfNeeded()
        {
            if (_appliedPatch)
                return;

            On.RoR2.CombatDirector.OnEnable += static (orig, self) =>
            {
                orig(self);
                tryMultiplyDirectorCredits(self);
            };

            _appliedPatch = true;
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || CombatDirector.instancesList.Count > 0;
        }

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        public override void OnStart()
        {
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                tryMultiplyDirectorCredits(director);
            }

            applyPatchIfNeeded();
        }

        public override void OnEnd()
        {
            _directorAppliedCounts.Clear();
        }

        static void tryMultiplyDirectorCredits(CombatDirector director)
        {
            if (!director)
                return;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            CombatDirector.DirectorMoneyWave[] moneyWaves = director.moneyWaves;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            if (moneyWaves == null || moneyWaves.Length <= 0)
                return;

            int numActivationsThisStage = _effectInfo.GetActivationCount(EffectActivationCountMode.PerStage);
            if (numActivationsThisStage <= 0)
                return;

            int totalAppliedToCount;
            if (!_directorAppliedCounts.TryGetValue(director, out totalAppliedToCount))
                totalAppliedToCount = 0;

            int missingApplyCount = numActivationsThisStage - totalAppliedToCount;
            if (missingApplyCount <= 0)
                return;

            float totalMultiplier = Mathf.Pow(CREDIT_MULTIPLIER, missingApplyCount);

            foreach (CombatDirector.DirectorMoneyWave moneyWave in moneyWaves)
            {
                moneyWave.multiplier *= totalMultiplier;
            }

            _directorAppliedCounts[director] = numActivationsThisStage;

#if DEBUG
            Log.Debug($"multiplied {nameof(CombatDirector)} {director} ({director.customName}) credits by {totalMultiplier} (applied {missingApplyCount} effect activations)");
#endif
        }
    }
}
