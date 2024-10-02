using UnityEngine;

namespace RiskOfChaos.ModifierController
{
    public sealed class ValueModificationManagerLocalFields : MonoBehaviour, IValueModificationFieldsProvider
    {
        public bool AnyModificationActive { get; set; }
    }
}