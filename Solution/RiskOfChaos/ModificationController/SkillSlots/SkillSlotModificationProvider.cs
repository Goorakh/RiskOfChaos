using RiskOfChaos.Content;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.SkillSlots
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class SkillSlotModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        public ValueModificationConfigBinding<float> CooldownMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setCooldownMultiplier))]
        public float CooldownMultiplier = 1f;

        public ValueModificationConfigBinding<int> StockAddConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setStockAdd))]
        public int StockAdd;

        [SyncVar(hook = nameof(setLockedSlots))]
        public SkillSlotMask LockedSlots;

        [SyncVar(hook = nameof(setForceActivatedSlots))]
        public SkillSlotMask ForceActivatedSlots;

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            CooldownMultiplierConfigBinding = new ValueModificationConfigBinding<float>(v => CooldownMultiplier = v);
            StockAddConfigBinding = new ValueModificationConfigBinding<int>(v => StockAdd = v);
        }

        void OnDestroy()
        {
            disposeConfigBindings();
        }

        void onRetire()
        {
            disposeConfigBindings();
        }

        void disposeConfigBindings()
        {
            CooldownMultiplierConfigBinding?.Dispose();
            StockAddConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setCooldownMultiplier(float cooldownMultiplier)
        {
            CooldownMultiplier = cooldownMultiplier;
            onValueChanged();
        }

        void setStockAdd(int stockAdd)
        {
            StockAdd = stockAdd;
            onValueChanged();
        }

        void setLockedSlots(SkillSlotMask lockedSlots)
        {
            LockedSlots = lockedSlots;
            onValueChanged();
        }

        void setForceActivatedSlots(SkillSlotMask forceActivatedSlots)
        {
            ForceActivatedSlots = forceActivatedSlots;
            onValueChanged();
        }
    }
}
