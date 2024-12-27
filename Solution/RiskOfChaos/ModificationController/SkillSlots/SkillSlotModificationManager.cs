using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
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

        public delegate void SkillSlotStateChangedDelegate(SkillSlot slot);

        public static event SkillSlotStateChangedDelegate OnSkillSlotLocked;
        public static event SkillSlotStateChangedDelegate OnSkillSlotUnlocked;

        public static event SkillSlotStateChangedDelegate OnSkillSlotStartForced;
        public static event SkillSlotStateChangedDelegate OnSkillSlotEndForced;

        public static event Action OnCooldownMultiplierChanged;

        public static event Action OnStockAddChanged;

        ValueModificationProviderHandler<SkillSlotModificationProvider> _modificationProviderHandler;

        float _cooldownMultiplier = 1f;
        public float CooldownMultiplier
        {
            get
            {
                return _cooldownMultiplier;
            }
            private set
            {
                if (_cooldownMultiplier == value)
                    return;

                _cooldownMultiplier = value;
                OnCooldownMultiplierChanged?.Invoke();
            }
        }

        int _stockAdd;
        public int StockAdd
        {
            get
            {
                return _stockAdd;
            }
            private set
            {
                if (_stockAdd == value)
                    return;

                _stockAdd = value;
                OnStockAddChanged?.Invoke();
            }
        }

        SkillSlotMask _lockedSlots;
        public SkillSlotMask LockedSlots
        {
            get
            {
                return _lockedSlots;
            }
            private set
            {
                if (_lockedSlots == value)
                    return;

                SkillSlotMask previouslyLockedSlots = _lockedSlots;
                _lockedSlots = value;

                SkillSlotMask changedLockedSlots = previouslyLockedSlots ^ _lockedSlots;
                foreach (SkillSlot changedSlot in changedLockedSlots)
                {
                    if (!previouslyLockedSlots.Contains(changedSlot))
                    {
                        Log.Debug($"Set skill slot '{changedSlot}' locked");

                        OnSkillSlotLocked?.Invoke(changedSlot);
                    }
                    else
                    {
                        Log.Debug($"Set skill slot '{changedSlot}' unlocked");

                        OnSkillSlotUnlocked?.Invoke(changedSlot);
                    }
                }
            }
        }

        SkillSlotMask _forceActivatedSlots;
        public SkillSlotMask ForceActivatedSlots
        {
            get
            {
                return _forceActivatedSlots;
            }
            private set
            {
                if (_forceActivatedSlots == value)
                    return;

                SkillSlotMask previouslyForceActivatedSlots = _forceActivatedSlots;
                _forceActivatedSlots = value;

                SkillSlotMask changedForceActivatedSlots = previouslyForceActivatedSlots ^ _forceActivatedSlots;
                foreach (SkillSlot changedSlot in changedForceActivatedSlots)
                {
                    if (!previouslyForceActivatedSlots.Contains(changedSlot))
                    {
                        Log.Debug($"Set skill slot '{changedSlot}' force activated");

                        OnSkillSlotStartForced?.Invoke(changedSlot);
                    }
                    else
                    {
                        Log.Debug($"Set skill slot '{changedSlot}' not force activated");

                        OnSkillSlotEndForced?.Invoke(changedSlot);
                    }
                }
            }
        }

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
        }
    }
}
