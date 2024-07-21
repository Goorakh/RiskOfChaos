using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.AttackDelay
{
    public sealed class SyncAttackDelayModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [field: SyncVar]
        public bool AnyModificationActive { get; set; }

        [SyncVar]
        public float TotalAttackDelay = 0f;
    }
}
