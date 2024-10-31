using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Director
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class DirectorModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> CombatDirectorCreditMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setCombatDirectorCreditMultiplier))]
        public float CombatDirectorCreditMultiplier = 1f;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            CombatDirectorCreditMultiplierConfigBinding = new ValueModificationConfigBinding<float>(v => CombatDirectorCreditMultiplier = v);
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
            CombatDirectorCreditMultiplierConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setCombatDirectorCreditMultiplier(float multiplier)
        {
            CombatDirectorCreditMultiplier = multiplier;
            onValueChanged();
        }
    }
}
