using RoR2;
using System;
using System.Linq;
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

        static ModificationManagerInfo[] _modificationManagers = Array.Empty<ModificationManagerInfo>();

        static ModificationManagerInfo createFromAttribute(ValueModificationManagerAttribute attribute)
        {
            Type modificationManagerType = attribute.target;
            bool isNetworked = typeof(NetworkBehaviour).IsAssignableFrom(modificationManagerType);
            string name = modificationManagerType.Name;

            GameObject prefab = NetPrefabs.CreateEmptyPrefabObject(name, isNetworked);

            foreach (Type componentType in attribute.GetAdditionalComponentTypes())
            {
                prefab.AddComponent(componentType);
            }

            prefab.AddComponent(modificationManagerType);

            return new ModificationManagerInfo(prefab, isNetworked, name);
        }

        public static void Initialize()
        {
            _modificationManagers = HG.Reflection.SearchableAttribute.GetInstances<ValueModificationManagerAttribute>()
                                                                     .Cast<ValueModificationManagerAttribute>()
                                                                     .Select(createFromAttribute)
                                                                     .ToArray();

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
