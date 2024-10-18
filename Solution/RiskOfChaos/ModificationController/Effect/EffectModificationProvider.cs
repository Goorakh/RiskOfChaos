using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Effect
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class EffectModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        [SyncVar(hook = nameof(setDurationMultiplier))]
        public float DurationMultiplier = 1f;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setDurationMultiplier(float durationMultiplier)
        {
            DurationMultiplier = durationMultiplier;
            onValueChanged();
        }
    }
}
