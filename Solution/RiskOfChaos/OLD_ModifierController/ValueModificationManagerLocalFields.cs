using UnityEngine;

namespace RiskOfChaos.OLD_ModifierController
{
    public sealed class ValueModificationManagerLocalFields : MonoBehaviour, IValueModificationFieldsProvider
    {
        public bool AnyModificationActive { get; set; }
    }
}