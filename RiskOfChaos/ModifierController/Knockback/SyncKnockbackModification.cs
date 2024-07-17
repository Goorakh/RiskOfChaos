using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Knockback
{
    public sealed class SyncKnockbackModification : NetworkBehaviour
    {
        [SyncVar]
        public bool AnyModificationActive;

        [SyncVar]
        public float TotalKnockbackMultiplier = 1f;
    }
}
