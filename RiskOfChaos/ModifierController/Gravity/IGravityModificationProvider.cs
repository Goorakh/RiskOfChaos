using UnityEngine;

namespace RiskOfChaos.ModifierController.Gravity
{
    public interface IGravityModificationProvider
    {
        void ModifyGravity(ref Vector3 gravity);
    }
}
