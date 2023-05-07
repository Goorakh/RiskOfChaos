using BepInEx.Configuration;
using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("increase_director_credits", ConfigName = "Increase Director Credits", EffectWeightReductionPercentagePerActivation = 35f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: +50% Director Credits")]
    public sealed class IncreaseDirectorCredits : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _creditIncreaseConfig;
        const float CREDIT_INCREASE_DEFAULT_VALUE = 0.5f;

        static float creditIncrease
        {
            get
            {
                if (_creditIncreaseConfig == null)
                {
                    return CREDIT_INCREASE_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(0f, _creditIncreaseConfig.Value);
                }
            }
        }

        static float creditMultiplier
        {
            get
            {
                return 1f + creditIncrease;
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _creditIncreaseConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Credit Increase Amount"), CREDIT_INCREASE_DEFAULT_VALUE, new ConfigDescription("How much to increase director credits by"));

            addConfigOption(new StepSliderOption(_creditIncreaseConfig, new StepSliderConfig
            {
                formatString = "+{0:P0}",
                min = 0f,
                max = 2f,
                increment = 0.05f
            }));
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || CombatDirector.instancesList.Count > 0;
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[]
            {
                creditIncrease
            };
        }

        class DirectorCreditModificationTracker : MonoBehaviour
        {
            class ModificationData
            {
                public bool HasBeenModified;
            }

            readonly Dictionary<UnityObjectWrapperKey<CombatDirector>, ModificationData> _directorModifications = new Dictionary<UnityObjectWrapperKey<CombatDirector>, ModificationData>();

            BaseEffect _effectOwner;

            public static DirectorCreditModificationTracker GetModificationTracker(GameObject obj, BaseEffect owner)
            {
                foreach (DirectorCreditModificationTracker modificationTracker in obj.GetComponents<DirectorCreditModificationTracker>())
                {
                    if (modificationTracker._effectOwner == owner)
                    {
                        return modificationTracker;
                    }
                }

                return null;
            }

            public static bool HasModified(CombatDirector director, BaseEffect owner)
            {
                if (!director || owner == null)
                    return false;

                DirectorCreditModificationTracker modificationTracker = GetModificationTracker(director.gameObject, owner);
                if (!modificationTracker)
                    return false;

                if (!modificationTracker._directorModifications.TryGetValue(director, out ModificationData modificationData))
                    return false;

                return modificationData.HasBeenModified;
            }

            public static void MarkModified(CombatDirector director, BaseEffect owner, bool modified)
            {
                if (!director || owner == null)
                    return;

                DirectorCreditModificationTracker modificationTracker = GetModificationTracker(director.gameObject, owner);
                if (!modificationTracker)
                {
                    modificationTracker = director.gameObject.AddComponent<DirectorCreditModificationTracker>();
                    modificationTracker._effectOwner = owner;
                }

                if (!modificationTracker._directorModifications.TryGetValue(director, out ModificationData modificationData))
                {
                    modificationData = new ModificationData();
                    modificationTracker._directorModifications.Add(director, modificationData);
                }

                modificationData.HasBeenModified = modified;
            }
        }

        float _creditMultiplier;

        public override void OnStart()
        {
            _creditMultiplier = creditMultiplier;

            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                tryMultiplyDirectorCredits(director);
            }

            On.RoR2.CombatDirector.OnEnable += CombatDirector_OnEnable;
            On.RoR2.CombatDirector.OnDisable += CombatDirector_OnDisable;
        }

        public override void OnEnd()
        {
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                tryUndoMultiplication(director);
            }

            On.RoR2.CombatDirector.OnEnable -= CombatDirector_OnEnable;
            On.RoR2.CombatDirector.OnDisable -= CombatDirector_OnDisable;
        }

        void CombatDirector_OnDisable(On.RoR2.CombatDirector.orig_OnDisable orig, CombatDirector self)
        {
            orig(self);
            tryUndoMultiplication(self);
        }

        void tryUndoMultiplication(CombatDirector director)
        {
            if (DirectorCreditModificationTracker.HasModified(director, this))
            {
                multiplyCredits(director, 1f / _creditMultiplier);
                DirectorCreditModificationTracker.MarkModified(director, this, false);
            }
        }

        void CombatDirector_OnEnable(On.RoR2.CombatDirector.orig_OnEnable orig, CombatDirector self)
        {
            orig(self);
            tryMultiplyDirectorCredits(self);
        }

        void tryMultiplyDirectorCredits(CombatDirector director)
        {
            if (!director)
                return;

            if (!DirectorCreditModificationTracker.HasModified(director, this))
            {
                multiplyCredits(director, _creditMultiplier);
                DirectorCreditModificationTracker.MarkModified(director, this, true);
            }
        }

        static void multiplyCredits(CombatDirector director, float multiplier)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            CombatDirector.DirectorMoneyWave[] moneyWaves = director.moneyWaves;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            if (moneyWaves == null || moneyWaves.Length <= 0)
                return;

            foreach (CombatDirector.DirectorMoneyWave moneyWave in moneyWaves)
            {
                moneyWave.multiplier *= multiplier;
            }

#if DEBUG
            Log.Debug($"multiplied {nameof(CombatDirector)} {director} ({director.customName}) credits by {multiplier}");
#endif
        }
    }
}
