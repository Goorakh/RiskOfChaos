using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect(EFFECT_ID, EffectRepetitionWeightExponent = 50f)]
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

        static readonly HashSet<CombatDirector> _appliedToDirectors = new HashSet<CombatDirector>();

        static bool _appliedPatch = false;
        static void applyPatchIfNeeded()
        {
            if (_appliedPatch)
                return;

            On.RoR2.CombatDirector.OnEnable += static (orig, self) =>
            {
                orig(self);

                int numActivationsThisStage = ChaosEffectDispatcher.GetTotalStageEffectActivationCount(_effectIndex);
                if (numActivationsThisStage > 0)
                {
                    tryMultiplyDirectorCredits(self, Mathf.Pow(CREDIT_MULTIPLIER, numActivationsThisStage));
                }
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
            _appliedToDirectors.Clear();
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
                tryMultiplyDirectorCredits(director, CREDIT_MULTIPLIER);
            }

            applyPatchIfNeeded();
        }

        static void tryMultiplyDirectorCredits(CombatDirector director, float multiplier)
        {
            const string LOG_PREFIX = $"{nameof(IncreaseDirectorCredits)}.{nameof(tryMultiplyDirectorCredits)} ";

            if (_appliedToDirectors.Add(director))
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                foreach (CombatDirector.DirectorMoneyWave moneyWave in director.moneyWaves)
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                {
                    moneyWave.multiplier *= multiplier;
                }

#if DEBUG
                Log.Debug(LOG_PREFIX + $"multiplied {nameof(CombatDirector)} {director} ({director.customName}) credits by {multiplier}");
#endif
            }
        }
    }
}
