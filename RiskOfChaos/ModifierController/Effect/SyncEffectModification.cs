using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Effect
{
    public sealed class SyncEffectModification : NetworkBehaviour
    {
        [SyncVar]
        public bool AnyModificationActive;

        [SyncVar]
        public float DurationMultiplier = 1f;
    }
}
