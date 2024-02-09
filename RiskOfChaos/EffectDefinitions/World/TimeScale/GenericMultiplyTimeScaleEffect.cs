using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    public abstract class GenericMultiplyTimeScaleEffect : TimedEffect, ITimeScaleModificationProvider
    {
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
            TimeScaleModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);

            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            On.RoR2.Util.PlayAttackSpeedSound += Util_PlayAttackSpeedSound;

            markAllPlayerStatsDirty();
            OnValueDirty += markAllPlayerStatsDirty;
        }

        public override void OnEnd()
        {
            if (TimeScaleModificationManager.Instance)
            {
                TimeScaleModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }

            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
            On.RoR2.Util.PlayAttackSpeedSound -= Util_PlayAttackSpeedSound;

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

        void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (self.isPlayerControlled)
            {
                self.moveSpeed /= multiplier;
                self.attackSpeed /= multiplier;
                self.acceleration /= multiplier;
            }
        }

        uint Util_PlayAttackSpeedSound(On.RoR2.Util.orig_PlayAttackSpeedSound orig, string soundString, GameObject gameObject, float attackSpeedStat)
        {
            if (gameObject && gameObject.TryGetComponent(out CharacterBody characterBody) && characterBody.isPlayerControlled)
            {
                attackSpeedStat *= multiplier;
            }

            return orig(soundString, gameObject, attackSpeedStat);
        }
    }
}
