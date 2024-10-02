using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.TimeScale
{
    public sealed class SyncTimeScaleModification : NetworkBehaviour, IValueModificationFieldsProvider
    {
        [field: SyncVar]
        public bool AnyModificationActive { get; set; }
    }
}
