using HG;
using RiskOfChaos.Components;
using RiskOfChaos.Components.MaterialInterpolation;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ScreenEffect
{
    public sealed class ScreenEffectManager : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(LocalPrefabAssetCollection localPrefabs, NetworkedPrefabAssetCollection networkPrefabs)
        {
            // ScreenEffectManager
            {
                GameObject prefab = Prefabs.CreatePrefab(nameof(RoCContent.LocalPrefabs.ScreenEffectManager), [
                    typeof(SetDontDestroyOnLoad),
                    typeof(AutoCreateOnRunStart),
                    typeof(DestroyOnRunEnd),
                    typeof(ScreenEffectManager)
                ]);

                localPrefabs.Add(prefab);
            }

            // InterpolatedScreenEffect
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.InterpolatedScreenEffect), [
                    typeof(SetDontDestroyOnLoad),
                    typeof(DestroyOnRunEnd),
                    typeof(NetworkedInterpolationComponent),
                    typeof(NetworkedMaterialPropertyInterpolators),
                    typeof(MaterialInterpolator),
                    typeof(ScreenEffectComponent)
                ]);

                networkPrefabs.Add(prefab);
            }
        }

        static ScreenEffectManager _instance;
        public static ScreenEffectManager Instance => _instance;

        static ScreenEffectComponent[] _sharedScreenEffectsBuffer = new ScreenEffectComponent[1];

        readonly List<ScreenEffectComponent> _activeScreenEffects = [];

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            ScreenEffectComponent.OnScreenEffectEnableGlobal += registerScreenEffect;
            ScreenEffectComponent.OnScreenEffectDisableGlobal += unregisterScreenEffect;

            _activeScreenEffects.Clear();
            _activeScreenEffects.EnsureCapacity(ScreenEffectComponent.Instances.Count);
            foreach (ScreenEffectComponent screenEffect in ScreenEffectComponent.Instances)
            {
                registerScreenEffect(screenEffect);
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            ScreenEffectComponent.OnScreenEffectEnableGlobal -= registerScreenEffect;
            ScreenEffectComponent.OnScreenEffectDisableGlobal -= unregisterScreenEffect;

            _activeScreenEffects.Clear();
        }

        void registerScreenEffect(ScreenEffectComponent screenEffectComponent)
        {
            _activeScreenEffects.Add(screenEffectComponent);
        }

        void unregisterScreenEffect(ScreenEffectComponent screenEffectComponent)
        {
            _activeScreenEffects.Remove(screenEffectComponent);
        }

        public IList<ScreenEffectComponent> GetActiveScreenEffects(ScreenEffectType screenEffectType)
        {
            if (_activeScreenEffects.Count == 0 || screenEffectType <= ScreenEffectType.Invalid || screenEffectType >= ScreenEffectType.Count)
                return Array.Empty<ScreenEffectComponent>();

            ArrayUtils.EnsureCapacity(ref _sharedScreenEffectsBuffer, _activeScreenEffects.Count);
            int activeScreenEffectCount = 0;
            foreach (ScreenEffectComponent screenEffect in _activeScreenEffects)
            {
                if (!screenEffect)
                    continue;

                ScreenEffectDef screenEffectDef = screenEffect.ScreenEffectDef;
                if (screenEffectDef == null || screenEffectDef.EffectType != screenEffectType)
                    continue;

                _sharedScreenEffectsBuffer[activeScreenEffectCount++] = screenEffect;
            }

            return new ArraySegment<ScreenEffectComponent>(_sharedScreenEffectsBuffer, 0, activeScreenEffectCount);
        }
    }
}
