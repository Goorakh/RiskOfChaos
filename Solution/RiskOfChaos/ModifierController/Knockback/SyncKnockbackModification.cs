using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Knockback
{
    public sealed class SyncKnockbackModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [field: SyncVar]
        public bool AnyModificationActive { get; set; }

        [SyncVar]
        public float TotalKnockbackMultiplier = 1f;
    }
}
