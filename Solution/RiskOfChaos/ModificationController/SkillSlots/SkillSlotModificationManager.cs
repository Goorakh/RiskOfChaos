using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Trackers;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ModificationController.SkillSlots
{
    public sealed class SkillSlotModificationManager : MonoBehaviour
    {
        static SkillSlotModificationManager _instance;
        public static SkillSlotModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // SkillSlotModificationProvider
            {
                GameObject prefab = Prefabs.CreateNetworkedValueModificationProviderPrefab(typeof(SkillSlotModificationProvider), nameof(RoCContent.NetworkedPrefabs.SkillSlotModificationProvider), false);

                networkPrefabs.Add(prefab);
            }
        }

        public delegate void SkillSlotLockedDelegate(SkillSlot slot);

        public static event SkillSlotLockedDelegate OnSkillSlotLocked;
        public static event SkillSlotLockedDelegate OnSkillSlotUnlocked;

        public static event Action OnCooldownMultiplierChanged;

        public static event Action OnStockAddChanged;

        ValueModificationProviderHandler<SkillSlotModificationProvider> _modificationProviderHandler;

        public float CooldownMultiplier { get; private set; }

        public int StockAdd { get; private set; }

        public SkillSlotMask LockedSlots { get; private set; }

        public SkillSlotMask ForceActivatedSlots { get; private set; }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _modificationProviderHandler = new ValueModificationProviderHandler<SkillSlotModificationProvider>(refreshValueModifications);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            if (_modificationProviderHandler != null)
            {
                _modificationProviderHandler.Dispose();
                _modificationProviderHandler = null;
            }
        }

        void refreshValueModifications(IReadOnlyCollection<SkillSlotModificationProvider> modificationProviders)
        {
            float previousCooldownMultiplier = CooldownMultiplier;
            int previousStockAdd = StockAdd;
            SkillSlotMask prevouslyLockedSlots = LockedSlots;

            float cooldownMultiplier = 1f;
            int stockAdds = 0;
            SkillSlotMask lockedSlots = new SkillSlotMask();
            SkillSlotMask forceActivatedSlots = new SkillSlotMask();

            foreach (SkillSlotModificationProvider modificationProvider in modificationProviders)
            {
                cooldownMultiplier *= modificationProvider.CooldownMultiplier;
                stockAdds += modificationProvider.StockAdd;
                lockedSlots |= modificationProvider.LockedSlots;
                forceActivatedSlots |= modificationProvider.ForceActivatedSlots;
            }

            CooldownMultiplier = Mathf.Max(0f, cooldownMultiplier);
            StockAdd = stockAdds;
            LockedSlots = lockedSlots;
            ForceActivatedSlots = forceActivatedSlots;

            if (Mathf.Abs(CooldownMultiplier - previousCooldownMultiplier) > 0.001f)
            {
                OnCooldownMultiplierChanged?.Invoke();
            }

            if (StockAdd != previousStockAdd)
            {
                OnStockAddChanged?.Invoke();
            }

            SkillSlotMask changedLockedSlots = prevouslyLockedSlots ^ LockedSlots;
            foreach (SkillSlot changedSlot in changedLockedSlots)
            {
                if (!prevouslyLockedSlots.Contains(changedSlot))
                {
#if DEBUG
                    Log.Debug($"Set skill slot '{changedSlot}' locked");
#endif

                    OnSkillSlotLocked?.Invoke(changedSlot);
                }
                else
                {
#if DEBUG
                    Log.Debug($"Set skill slot '{changedSlot}' unlocked");
#endif

                    OnSkillSlotUnlocked?.Invoke(changedSlot);
                }
            }
        }
    }
}
