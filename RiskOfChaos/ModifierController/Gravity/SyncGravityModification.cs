using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Gravity
{
    public sealed class SyncGravityModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [field: SyncVar]
        public bool AnyModificationActive { get; set; }
    }
}
