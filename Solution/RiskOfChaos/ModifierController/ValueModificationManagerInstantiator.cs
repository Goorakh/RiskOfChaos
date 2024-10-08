using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController
{
    public static class ValueModificationManagerInstantiator
    {
        readonly struct ModificationManagerInfo(GameObject Prefab, bool IsNetworked, string Name)
        {
            public readonly GameObject Instantiate()
            {
                GameObject modificationManager = GameObject.Instantiate(Prefab);

                if (IsNetworked)
                {
                    NetworkServer.Spawn(modificationManager);
                }

#if DEBUG
                Log.Debug($"Created modification manager: {Name} (networked={IsNetworked})");
#endif

                return modificationManager;
            }
        }

        static ModificationManagerInfo[] _modificationManagers = [];

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkedPrefabs, LocalPrefabAssetCollection localPrefabs)
        {
            List<HG.Reflection.SearchableAttribute> modificationManagerAttributes = HG.Reflection.SearchableAttribute.GetInstances<ValueModificationManagerAttribute>();
            List<ModificationManagerInfo> modificationManagers = new List<ModificationManagerInfo>(modificationManagerAttributes.Count);

            foreach (HG.Reflection.SearchableAttribute modificationManagerAttribute in modificationManagerAttributes)
            {
                if (modificationManagerAttribute is not ValueModificationManagerAttribute attribute)
                    continue;

                Type modificationManagerType = attribute.target;
                string name = modificationManagerType.Name;

                Type[] requiredComponentTypes = [.. attribute.GetAdditionalComponentTypes(), modificationManagerType];
                requiredComponentTypes = RequiredComponentsAttribute.ResolveRequiredComponentTypes(requiredComponentTypes);

                bool isNetworked = typeof(NetworkBehaviour).IsAssignableFrom(modificationManagerType);
                foreach (Type componentType in requiredComponentTypes)
                {
                    isNetworked |= typeof(NetworkBehaviour).IsAssignableFrom(componentType);
                }

                GameObject prefab;
                AssetCollection<GameObject> prefabAssetCollection;
                if (isNetworked)
                {
                    prefab = Prefabs.CreateNetworkedPrefab(name, 0x0684FDB9, requiredComponentTypes);
                    prefabAssetCollection = networkedPrefabs;
                }
                else
                {
                    prefab = Prefabs.CreatePrefab(name, requiredComponentTypes);
                    prefabAssetCollection = localPrefabs;
                }

                modificationManagers.Add(new ModificationManagerInfo(prefab, isNetworked, name));
                prefabAssetCollection.Add(prefab);
            }

            _modificationManagers = modificationManagers.ToArray();

            Run.onRunStartGlobal += Run_onRunStartGlobal;
        }

        static void Run_onRunStartGlobal(Run _)
        {
            if (!NetworkServer.active)
                return;

            foreach (ModificationManagerInfo modificationManager in _modificationManagers)
            {
                modificationManager.Instantiate();
            }
        }
    }
}
