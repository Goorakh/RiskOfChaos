using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos
{
    public static class NetPrefabs
    {
        public static GameObject GenericTeamInventoryPrefab { get; private set; }

        internal static void InitializeAll()
        {
            // GenericTeamInventoryPrefab
            {
                const string INVENTORY_PREFAB_NAME = Main.PluginGUID + "_GenericTeamInventory";

                GameObject inventoryPrefab = new GameObject(INVENTORY_PREFAB_NAME);

                inventoryPrefab.AddComponent<NetworkIdentity>();
                inventoryPrefab.AddComponent<SetDontDestroyOnLoad>();
                inventoryPrefab.AddComponent<TeamFilter>();
                inventoryPrefab.AddComponent<Inventory>();
                inventoryPrefab.AddComponent<EnemyInfoPanelInventoryProvider>();

                GenericTeamInventoryPrefab = inventoryPrefab.InstantiateClone(INVENTORY_PREFAB_NAME);
                GameObject.Destroy(inventoryPrefab);
            }
        }
    }
}
