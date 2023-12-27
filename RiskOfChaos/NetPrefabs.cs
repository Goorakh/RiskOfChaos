using R2API;
using RiskOfChaos.Components;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Networking.Components.Effects;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos
{
    public static class NetPrefabs
    {
        public static GameObject GenericTeamInventoryPrefab { get; private set; }

        public static GameObject MonsterItemStealControllerPrefab { get; private set; }

        static readonly string[] _geyserPrefabPaths = new string[]
        {
            "RoR2/Base/Common/Props/Geyser.prefab",
            "RoR2/Base/artifactworld/AWGeyser.prefab",
            "RoR2/Base/moon/MoonGeyser.prefab",
            "RoR2/DLC1/ancientloft/AncientLoft_Geyser.prefab",
            "RoR2/DLC1/snowyforest/SFGeyser.prefab"
        };
        public static GameObject[] GeyserPrefabs { get; private set; } = Array.Empty<GameObject>();

        public static GameObject EffectsNetworkerPrefab { get; private set; }

        public static GameObject ItemStealerPositionIndicatorPrefab { get; private set; }

        public static GameObject SulfurPodBasePrefab { get; private set; }

        public static GameObject DummyDamageInflictorPrefab { get; private set; }

        public static GameObject ConfigNetworkerPrefab { get; private set; }

        public static GameObject CreateEmptyPrefabObject(string name, bool networked = true)
        {
            GameObject tmp = new GameObject(name);

            if (networked)
            {
                tmp.AddComponent<NetworkIdentity>();
            }

            GameObject prefab = tmp.InstantiateClone(Main.PluginGUID + "_" + name, networked);
            GameObject.Destroy(tmp);

            return prefab;
        }

        internal static void InitializeAll()
        {
            // GenericTeamInventoryPrefab
            {
                GenericTeamInventoryPrefab = CreateEmptyPrefabObject("GenericTeamInventory");

                GenericTeamInventoryPrefab.AddComponent<SetDontDestroyOnLoad>();
                GenericTeamInventoryPrefab.AddComponent<TeamFilter>();
                GenericTeamInventoryPrefab.AddComponent<Inventory>();
                GenericTeamInventoryPrefab.AddComponent<EnemyInfoPanelInventoryProvider>();
                GenericTeamInventoryPrefab.AddComponent<DestroyOnRunEnd>();
            }

            // MonsterItemStealControllerPrefab
            {
                GameObject itemStealControllerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/ItemStealController.prefab").WaitForCompletion();

                MonsterItemStealControllerPrefab = itemStealControllerPrefab.InstantiateClone(Main.PluginGUID + "_" + "MonsterItemStealController", true);

                NetworkedBodyAttachment networkedBodyAttachment = MonsterItemStealControllerPrefab.GetComponent<NetworkedBodyAttachment>();
                networkedBodyAttachment.shouldParentToAttachedBody = true;

                ItemStealController itemStealController = MonsterItemStealControllerPrefab.GetComponent<ItemStealController>();
                itemStealController.stealInterval = 0.2f;

                MonsterItemStealControllerPrefab.AddComponent<SyncStolenItemCount>();
                MonsterItemStealControllerPrefab.AddComponent<ShowStolenItemsPositionIndicator>();
            }

            // GeyserPrefabs
            {
                int geyserCount = _geyserPrefabPaths.Length;
                GeyserPrefabs = new GameObject[geyserCount];
                for (int i = 0; i < geyserCount; i++)
                {
                    GameObject geyserPrefab = Addressables.LoadAssetAsync<GameObject>(_geyserPrefabPaths[i]).WaitForCompletion();
                    string prefabName = geyserPrefab.name;

                    GameObject geyserHolderPrefab = new GameObject(Main.PluginGUID + "_Networked" + prefabName);
                    GameObject geyser = GameObject.Instantiate(geyserPrefab);
                    geyser.transform.SetParent(geyserHolderPrefab.transform, true);
                    geyser.transform.localPosition = Vector3.zero;

                    geyserHolderPrefab.AddComponent<NetworkIdentity>();
                    geyserHolderPrefab.AddComponent<SyncJumpVolumeVelocity>();
                    GeyserPrefabs[i] = geyserHolderPrefab.InstantiateClone(Main.PluginGUID + "_Networked" + prefabName, true);

                    GameObject.Destroy(geyserHolderPrefab);
                }
            }

            // EffectsNetworkerPrefab
            {
                EffectsNetworkerPrefab = CreateEmptyPrefabObject("EffectsNetworker");

                EffectsNetworkerPrefab.AddComponent<SetDontDestroyOnLoad>();
                EffectsNetworkerPrefab.AddComponent<DestroyOnRunEnd>();
                EffectsNetworkerPrefab.AddComponent<ActiveTimedEffectsProvider>();
                EffectsNetworkerPrefab.AddComponent<NextEffectProvider>();
            }

            // ItemStealerPositionIndicatorPrefab
            {
                ItemStealerPositionIndicatorPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/BossPositionIndicator.prefab").WaitForCompletion().InstantiateClone("ItemStealerPositionIndicator", false);

                PositionIndicator positionIndicator = ItemStealerPositionIndicatorPrefab.GetComponent<PositionIndicator>();

                if (positionIndicator.insideViewObject)
                {
                    foreach (SpriteRenderer insideSprite in positionIndicator.insideViewObject.GetComponentsInChildren<SpriteRenderer>())
                    {
                        insideSprite.color = Color.cyan;
                    }
                }

                if (positionIndicator.outsideViewObject)
                {
                    foreach (SpriteRenderer outsideSprite in positionIndicator.outsideViewObject.GetComponentsInChildren<SpriteRenderer>())
                    {
                        outsideSprite.color = Color.cyan;
                    }
                }
            }

            // SulfurPodBasePrefab
            {
                GameObject prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/sulfurpools/SPSulfurPodBase.prefab").WaitForCompletion();
                string prefabName = prefab.name;

                GameObject tmp = GameObject.Instantiate(prefab);
                tmp.AddComponent<NetworkIdentity>();

                SulfurPodBasePrefab = tmp.InstantiateClone(Main.PluginGUID + "_Networked" + prefabName, true);

                GameObject.Destroy(tmp);
            }

            // DummyDamageInflictorPrefab
            {
                DummyDamageInflictorPrefab = CreateEmptyPrefabObject("DummyDamageInflictor");
                DummyDamageInflictorPrefab.AddComponent<SetDontDestroyOnLoad>();
                DummyDamageInflictorPrefab.AddComponent<DestroyOnRunEnd>();
                DummyDamageInflictorPrefab.AddComponent<DummyDamageInflictor>();
            }

            // ConfigNetworkerPrefab
            {
                ConfigNetworkerPrefab = CreateEmptyPrefabObject("ConfigNetworker");
                ConfigNetworkerPrefab.AddComponent<SetDontDestroyOnLoad>();
                ConfigNetworkerPrefab.AddComponent<DestroyOnRunEnd>();
                ConfigNetworkerPrefab.AddComponent<SyncConfigValue>();
            }

            Run.onRunStartGlobal += onRunStart;
        }

        static void onRunStart(Run _)
        {
            if (!NetworkServer.active)
                return;

            NetworkServer.Spawn(GameObject.Instantiate(EffectsNetworkerPrefab));

            NetworkServer.Spawn(GameObject.Instantiate(DummyDamageInflictorPrefab));
        }
    }
}