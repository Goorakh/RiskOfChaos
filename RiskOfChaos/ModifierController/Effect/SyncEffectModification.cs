using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Effect
{
    public sealed class SyncEffectModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [field: SyncVar]
        public bool AnyModificationActive { get; set; }

        [SyncVar]
        public float DurationMultiplier = 1f;
    }
}
