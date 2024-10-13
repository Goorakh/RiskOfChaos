using UnityEngine.Networking;

namespace RiskOfChaos.OLD_ModifierController.Gravity
{
    public sealed class SyncGravityModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [field: SyncVar]
        public bool AnyModificationActive { get; set; }
    }
}
