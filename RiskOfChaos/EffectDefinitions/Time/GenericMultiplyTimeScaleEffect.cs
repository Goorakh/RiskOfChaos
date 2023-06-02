using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.TimeScale;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Time
{
    public abstract class GenericMultiplyTimeScaleEffect : TimedEffect, ITimeScaleModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return TimeScaleModificationManager.Instance;
        }

        protected abstract float multiplier { get; }

        public bool ContributeToPlayerRealtimeTimeScalePatch => true;

        public abstract event Action OnValueDirty;

        public void ModifyValue(ref float value)
        {
            value *= multiplier;
        }

        public override void OnStart()
        {
            TimeScaleModificationManager.Instance.RegisterModificationProvider(this);

            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            markAllPlayerStatsDirty();
        }

        public override void OnEnd()
        {
            if (TimeScaleModificationManager.Instance)
            {
                TimeScaleModificationManager.Instance.UnregisterModificationProvider(this);
            }

            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
            markAllPlayerStatsDirty();
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
    }
}
