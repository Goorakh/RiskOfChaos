using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.AttackDelay
{
    public sealed class SyncAttackDelayModification : NetworkBehaviour
    {
        [SyncVar]
        public bool AnyModificationActive;

        [SyncVar]
        public float TotalAttackDelay = 0f;
    }
}
