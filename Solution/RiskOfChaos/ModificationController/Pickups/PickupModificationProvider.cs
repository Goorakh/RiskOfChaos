using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Pickups
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class PickupModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<int> BounceCountConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setBounceCount))]
        public int BounceCount;

        public ValueModificationConfigBinding<float> SpawnCountMultiplierConfigBinding { get; private set; }

        [SyncVar]
        public float SpawnCountMultiplier = 1f;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            BounceCountConfigBinding = new ValueModificationConfigBinding<int>(setBounceCountFromConfig);
            SpawnCountMultiplierConfigBinding = new ValueModificationConfigBinding<float>(setSpawnCountMultiplierFromConfig);
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
            BounceCountConfigBinding?.Dispose();
            SpawnCountMultiplierConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        [Server]
        void setBounceCountFromConfig(int bounceCount)
        {
            BounceCount = bounceCount;
        }

        void setBounceCount(int bounceCount)
        {
            BounceCount = bounceCount;
            onValueChanged();
        }

        [Server]
        void setSpawnCountMultiplierFromConfig(float spawnCountMultiplier)
        {
            SpawnCountMultiplier = spawnCountMultiplier;
        }

        void setSpawnCountMultiplier(float spawnCountMultiplier)
        {
            SpawnCountMultiplier = spawnCountMultiplier;
            onValueChanged();
        }
    }
}
