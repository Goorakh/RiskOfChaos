using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect(EFFECT_ID, EffectRepetitionWeightExponent = 35f)]
    public class IncreaseDirectorCredits : BaseEffect
    {
        const string EFFECT_ID = "IncreaseDirectorCredits";

        const float CREDIT_MULTIPLIER = 1.5f;

        static int _effectIndex;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _effectIndex = ChaosEffectCatalog.FindEffectIndex(EFFECT_ID);
        }

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

            Stage.onServerStageComplete += static _ =>
            {
                clearAppliedToDirectors();
            };

            Run.onRunDestroyGlobal += static _ =>
            {
                clearAppliedToDirectors();
            };

            _appliedPatch = true;
        }

        static void clearAppliedToDirectors()
        {
            _directorAppliedCounts.Clear();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return CombatDirector.instancesList.Count > 0;
        }

        public override void OnStart()
        {
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                tryMultiplyDirectorCredits(director);
            }

            applyPatchIfNeeded();
        }

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
        static void tryMultiplyDirectorCredits(CombatDirector director)
        {
            if (!director || director.moneyWaves == null)
                return;

            int numActivationsThisStage = ChaosEffectDispatcher.GetTotalStageEffectActivationCount(_effectIndex);
            if (numActivationsThisStage <= 0)
                return;

            int totalAppliedToCount;
            if (!_directorAppliedCounts.TryGetValue(director, out totalAppliedToCount))
                totalAppliedToCount = 0;

            int missingApplyCount = numActivationsThisStage - totalAppliedToCount;
            if (missingApplyCount <= 0)
                return;

            float totalMultiplier = Mathf.Pow(CREDIT_MULTIPLIER, missingApplyCount);

            foreach (CombatDirector.DirectorMoneyWave moneyWave in director.moneyWaves)
            {
                moneyWave.multiplier *= totalMultiplier;
            }

            _directorAppliedCounts[director] = numActivationsThisStage;

#if DEBUG
            Log.Debug($"multiplied {nameof(CombatDirector)} {director} ({director.customName}) credits by {totalMultiplier} (missed {missingApplyCount} activations)");
#endif
        }
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
    }
}
