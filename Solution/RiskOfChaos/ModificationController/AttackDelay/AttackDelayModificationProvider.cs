using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.AttackDelay
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class AttackDelayModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        [SyncVar(hook = nameof(setDelay))]
        public float Delay;

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

        void setDelay(float delay)
        {
            Delay = delay;
            onValueChanged();
        }
    }
}
