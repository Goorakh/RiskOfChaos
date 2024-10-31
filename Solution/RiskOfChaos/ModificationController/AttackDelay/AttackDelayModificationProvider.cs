using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.AttackDelay
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class AttackDelayModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> DelayConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setDelay))]
        public float Delay;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            DelayConfigBinding = new ValueModificationConfigBinding<float>(setDelayFromConfig);
        }

        void OnDestroy()
        {
            _modificationController.OnRetire -= onRetire;
            disposeConfigBindings();
        }

        void onRetire()
        {
            disposeConfigBindings();
        }

        void disposeConfigBindings()
        {
            DelayConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        [Server]
        void setDelayFromConfig(float delay)
        {
            Delay = delay;
        }

        void setDelay(float delay)
        {
            Delay = delay;
            onValueChanged();
        }
    }
}
