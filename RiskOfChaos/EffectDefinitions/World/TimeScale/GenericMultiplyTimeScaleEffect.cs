using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController;
using RiskOfChaos.ModifierController.CharacterStats;
using RiskOfChaos.ModifierController.TimeScale;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    public abstract class GenericMultiplyTimeScaleEffect : TimedEffect, ITimeScaleModificationProvider, ICharacterStatModificationProvider
    {
        event Action IValueModificationProvider<CharacterBody>.OnValueDirty
        {
            add
            {
                OnValueDirty += value;
            }
            remove
            {
                OnValueDirty -= value;
            }
        }

        event Action IValueModificationProvider<float>.OnValueDirty
        {
            add
            {
                OnValueDirty += value;
            }
            remove
            {
                OnValueDirty -= value;
            }
        }

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

        public void ModifyValue(ref CharacterBody value)
        {
            if (value.isPlayerControlled)
            {
                value.moveSpeed /= multiplier;
                value.attackSpeed /= multiplier;
                value.acceleration /= multiplier;
            }
        }

        public override void OnStart()
        {
            TimeScaleModificationManager.Instance.RegisterModificationProvider(this);

            if (CharacterStatModificationManager.Instance)
            {
                CharacterStatModificationManager.Instance.RegisterModificationProvider(this);
            }
        }

        public override void OnEnd()
        {
            if (TimeScaleModificationManager.Instance)
            {
                TimeScaleModificationManager.Instance.UnregisterModificationProvider(this);
            }

            if (CharacterStatModificationManager.Instance)
            {
                CharacterStatModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
