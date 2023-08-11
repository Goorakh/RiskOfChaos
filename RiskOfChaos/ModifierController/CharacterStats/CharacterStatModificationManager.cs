using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.CharacterStats
{
    public sealed class CharacterStatModificationManager : MonoBehaviour, IValueModificationManager<ICharacterStatModificationProvider, CharacterBody>
    {
        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);

                if (!NetworkServer.active || !Instance)
                    return;

                foreach (ICharacterStatModificationProvider modificationProvider in Instance._activeModificationProviders)
                {
                    modificationProvider.ModifyValue(ref self);
                }
            };

            _hasAppliedPatches = true;
        }

        static CharacterStatModificationManager _instance;
        public static CharacterStatModificationManager Instance => _instance;

        public bool AnyModificationActive => _activeModificationProviders.Count > 0;

        public event Action OnValueModificationUpdated;

        readonly HashSet<ICharacterStatModificationProvider> _activeModificationProviders = new HashSet<ICharacterStatModificationProvider>();

        void Awake()
        {
            SingletonHelper.Assign(ref _instance, this);

            tryApplyPatches();
            OnValueModificationUpdated += markAllStatsDirty;
        }

        void OnDestroy()
        {
            SingletonHelper.Unassign(ref _instance, this);

            OnValueModificationUpdated -= markAllStatsDirty;
        }

        void markAllStatsDirty()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }

        public void RegisterModificationProvider(ICharacterStatModificationProvider provider)
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_activeModificationProviders.Add(provider))
            {
                OnValueModificationUpdated?.Invoke();
                provider.OnValueDirty += markAllStatsDirty;
            }
        }

        public void UnregisterModificationProvider(ICharacterStatModificationProvider provider)
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_activeModificationProviders.Remove(provider))
            {
                OnValueModificationUpdated?.Invoke();
                provider.OnValueDirty -= markAllStatsDirty;
            }
        }
    }
}
