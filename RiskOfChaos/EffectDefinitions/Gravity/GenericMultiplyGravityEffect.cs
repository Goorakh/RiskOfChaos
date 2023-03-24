using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    public abstract class GenericMultiplyGravityEffect : GenericGravityEffect
    {
        protected abstract float multiplier { get; }

        protected override sealed Vector3 modifyGravity(Vector3 originalGravity)
        {
            return originalGravity * multiplier;
        }
    }
}
