using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Gravity
{
    public abstract class GenericMultiplyGravityEffect : GenericGravityEffect
    {
        protected abstract float multiplier { get; }

        public override void ModifyValue(ref Vector3 gravity)
        {
            gravity *= multiplier;
        }
    }
}
