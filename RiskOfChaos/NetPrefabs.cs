using R2API;
using RiskOfChaos.Components;
using RiskOfChaos.Networking.Components;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos
{
    public static class NetPrefabs
    {
        public static GameObject GenericTeamInventoryPrefab { get; private set; }

        public static GameObject GravityNetSyncerPrefab { get; private set; }

        static GameObject createPrefabObject(string name, bool networked = true)
        {
            GameObject tmp = new GameObject(name);
            GameObject prefab = tmp.InstantiateClone(Main.PluginGUID + "_" + name, networked);
            GameObject.Destroy(tmp);

            return prefab;
        }

        internal static void InitializeAll()
        {
            // GenericTeamInventoryPrefab
            {
                GenericTeamInventoryPrefab = createPrefabObject("GenericTeamInventory");

                GenericTeamInventoryPrefab.AddComponent<NetworkIdentity>();
                GenericTeamInventoryPrefab.AddComponent<SetDontDestroyOnLoad>();
                GenericTeamInventoryPrefab.AddComponent<TeamFilter>();
                GenericTeamInventoryPrefab.AddComponent<Inventory>();
                GenericTeamInventoryPrefab.AddComponent<EnemyInfoPanelInventoryProvider>();
                GenericTeamInventoryPrefab.AddComponent<DestroyOnRunEnd>();
            }
        }
    }
}
