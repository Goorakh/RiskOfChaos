using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    public abstract class GenericMultiplyTimeScaleEffect : TimedEffect, ITimeScaleModificationProvider
    {
        static bool _appliedPatches;

        static void applyPatches()
        {
            if (_appliedPatches)
                return;

            static float getTotalTimeScaleMultiplier()
            {
                float multiplier = 1f;

                if (NetworkServer.active && ChaosEffectTracker.Instance)
                {
                    foreach (GenericMultiplyTimeScaleEffect effect in ChaosEffectTracker.Instance.OLD_GetActiveEffectInstancesOfType<GenericMultiplyTimeScaleEffect>())
                    {
                        multiplier *= effect.multiplier;
                    }
                }

                return multiplier;
            }

            CharacterBodyRecalculateStatsHook.PostRecalculateStats += (body) =>
            {
                if (body.isPlayerControlled)
                {
                    float timeScaleMultiplier = getTotalTimeScaleMultiplier();

                    body.moveSpeed /= timeScaleMultiplier;
                    body.attackSpeed /= timeScaleMultiplier;
                    body.acceleration /= timeScaleMultiplier;
                }
            };

            On.RoR2.Util.PlayAttackSpeedSound += (orig, soundString, gameObject, attackSpeedStat) =>
            {
                if (gameObject && gameObject.TryGetComponent(out CharacterBody characterBody) && characterBody.isPlayerControlled)
                {
                    attackSpeedStat *= getTotalTimeScaleMultiplier();
                }

                return orig(soundString, gameObject, attackSpeedStat);
            };

            _appliedPatches = true;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return TimeScaleModificationManager.Instance;
        }

        protected abstract float multiplier { get; }

        public abstract event Action OnValueDirty;

        public void ModifyValue(ref float value)
        {
            value *= multiplier;
        }

        public override void OnStart()
        {
            applyPatches();

            TimeScaleModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);

            markAllPlayerStatsDirty();
            OnValueDirty += markAllPlayerStatsDirty;
        }

        public override void OnEnd()
        {
            if (TimeScaleModificationManager.Instance)
            {
                TimeScaleModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }

            markAllPlayerStatsDirty();
            OnValueDirty -= markAllPlayerStatsDirty;
        }

        static void markAllPlayerStatsDirty()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body && body.isPlayerControlled)
                {
                    body.MarkAllStatsDirty();
                }
            }
        }
    }
}
