using UnityEngine;

namespace RiskOfChaos.Components.MaterialInterpolation
{
    public interface IMaterialPropertyInterpolator
    {
        void SetValues(Material material, float interpolationFraction);
    }
}
