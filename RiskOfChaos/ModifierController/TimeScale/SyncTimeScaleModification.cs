using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.TimeScale
{
    public sealed class SyncTimeScaleModification : NetworkBehaviour
    {
        [SyncVar]
        public bool AnyModificationActive;
    }
}
