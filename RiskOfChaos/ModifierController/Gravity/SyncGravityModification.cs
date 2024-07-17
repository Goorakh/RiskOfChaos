using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Gravity
{
    public sealed class SyncGravityModification : NetworkBehaviour
    {
        [SyncVar]
        public bool AnyModificationActive;
    }
}
