using RiskOfChaos.Components;
using RiskOfChaos.Components.MaterialInterpolation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ScreenEffect
{
    public sealed class ScreenEffectComponent : NetworkBehaviour
    {
        static readonly List<ScreenEffectComponent> _instances = [];
        public static readonly ReadOnlyCollection<ScreenEffectComponent> Instances = new ReadOnlyCollection<ScreenEffectComponent>(_instances);

        public static event Action<ScreenEffectComponent> OnScreenEffectEnableGlobal;
        public static event Action<ScreenEffectComponent> OnScreenEffectDisableGlobal;

        [SyncVar(hook = nameof(setScreenEffectIndex))]
        int _screenEffectIndexInternal;

        IMaterialProvider _materialProvider;

        GenericInterpolationComponent _interpolationComponent;

        public Material ScreenEffectMaterial => _materialProvider?.Material;

        public ScreenEffectIndex ScreenEffectIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ScreenEffectIndex)(_screenEffectIndexInternal - 1);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _screenEffectIndexInternal = (int)value + 1;
        }

        public ScreenEffectDef ScreenEffectDef => ScreenEffectCatalog.GetScreenEffectDef(ScreenEffectIndex);

        public event Action OnMaterialPropertiesChanged;

        void Awake()
        {
            _interpolationComponent = GetComponent<GenericInterpolationComponent>();

            _materialProvider = GetComponent<IMaterialProvider>();

            if (_materialProvider == null)
            {
                Log.Error("Missing MaterialProvider component");
            }
        }

        void OnEnable()
        {
            _instances.Add(this);

            if (_materialProvider != null)
            {
                _materialProvider.OnPropertiesChanged += onMaterialPropertiesChanged;
            }

            OnScreenEffectEnableGlobal?.Invoke(this);
        }

        void OnDisable()
        {
            _instances.Remove(this);

            if (_materialProvider != null)
            {
                _materialProvider.OnPropertiesChanged -= onMaterialPropertiesChanged;
            }

            OnScreenEffectDisableGlobal?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            refreshMaterialInstance();
        }

        void setScreenEffectIndex(int screenEffectIndexInt)
        {
            _screenEffectIndexInternal = screenEffectIndexInt;
            refreshMaterialInstance();
        }

        void refreshMaterialInstance()
        {
            if (_materialProvider == null)
                return;

            _materialProvider.Material = ScreenEffectDef?.EffectMaterial;
        }

        void onMaterialPropertiesChanged()
        {
            OnMaterialPropertiesChanged?.Invoke();
        }

        [Server]
        public void Remove()
        {
            if (_interpolationComponent)
            {
                _interpolationComponent.InterpolateOutOrDestroy();
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
