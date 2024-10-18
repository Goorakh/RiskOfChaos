using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.ModificationController
{
    public sealed class ValueModificationProviderHandler<TProviderComponent> : IDisposable where TProviderComponent : MonoBehaviour
    {
        static readonly List<TProviderComponent> _sharedGetComponentsBuffer = new List<TProviderComponent>(1);

        public delegate void RefreshValueModificationsDelegate(IReadOnlyCollection<TProviderComponent> activeProviders);

        readonly List<TProviderComponent> _activeProviders;
        public readonly ReadOnlyCollection<TProviderComponent> ActiveProviders;

        readonly RefreshValueModificationsDelegate _refreshValueModificationsFunc;

        readonly bool _autoUpdate;

        bool _valueModificationsDirty;

        bool _isDisposed;

        public ValueModificationProviderHandler(RefreshValueModificationsDelegate refreshValueModificationsFunc, bool autoUpdate = true)
        {
            if (refreshValueModificationsFunc is null)
                throw new ArgumentNullException(nameof(refreshValueModificationsFunc));

            _refreshValueModificationsFunc = refreshValueModificationsFunc;

            _activeProviders = [];
            ActiveProviders = new ReadOnlyCollection<TProviderComponent>(_activeProviders);

            _autoUpdate = autoUpdate;
            if (_autoUpdate)
            {
                RoR2Application.onFixedUpdate += Update;
            }

            ValueModificationController.OnModificationControllerStartGlobal += tryRegisterModificationController;
            ValueModificationController.OnModificationControllerEndGlobal += tryUnregisterModificationController;

            _activeProviders.EnsureCapacity(ValueModificationController.Instances.Count);
            foreach (ValueModificationController modificationController in ValueModificationController.Instances)
            {
                tryRegisterModificationController(modificationController);
            }

            refreshValueModifications();
        }

        ~ValueModificationProviderHandler()
        {
            dispose();
        }

        public void Dispose()
        {
            dispose();
            GC.SuppressFinalize(this);
        }

        void dispose()
        {
            if (_isDisposed)
                return;

            ValueModificationController.OnModificationControllerStartGlobal -= tryRegisterModificationController;
            ValueModificationController.OnModificationControllerEndGlobal -= tryUnregisterModificationController;

            _activeProviders.Clear();

            if (_autoUpdate)
            {
                RoR2Application.onFixedUpdate -= Update;
            }

            _isDisposed = true;
        }

        public void Update()
        {
            if (_isDisposed)
                return;

            if (_valueModificationsDirty)
            {
                refreshValueModifications();
            }
        }

        void tryRegisterModificationController(ValueModificationController modificationController)
        {
            _sharedGetComponentsBuffer.Clear();
            modificationController.GetComponents(_sharedGetComponentsBuffer);

            if (_sharedGetComponentsBuffer.Count <= 0)
                return;

            bool anyProviderAdded = false;
            foreach (TProviderComponent providerComponent in _sharedGetComponentsBuffer)
            {
                modificationController.OnValuesDirty += MarkValueModificationsDirty;
                _activeProviders.Add(providerComponent);
                anyProviderAdded = true;
            }

            if (anyProviderAdded)
            {
                MarkValueModificationsDirty();
            }
        }

        void tryUnregisterModificationController(ValueModificationController modificationController)
        {
            _sharedGetComponentsBuffer.Clear();
            modificationController.GetComponents(_sharedGetComponentsBuffer);

            if (_sharedGetComponentsBuffer.Count <= 0)
                return;

            bool anyProviderRemoved = false;
            foreach (TProviderComponent providerComponent in _sharedGetComponentsBuffer)
            {
                modificationController.OnValuesDirty -= MarkValueModificationsDirty;

                if (_activeProviders.Remove(providerComponent))
                {
                    anyProviderRemoved = true;
                }
            }

            if (anyProviderRemoved)
            {
                MarkValueModificationsDirty();
            }
        }

        public void MarkValueModificationsDirty()
        {
            _valueModificationsDirty = true;
        }

        void refreshValueModifications()
        {
            _valueModificationsDirty = false;
            _refreshValueModificationsFunc(ActiveProviders);
        }
    }
}
