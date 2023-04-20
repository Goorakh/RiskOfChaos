using UnityEngine;

namespace RiskOfChaos.GravityModifier
{
    public interface IGravityModificationProvider
    {
        void ModifyGravity(ref Vector3 gravity);
    }
}
