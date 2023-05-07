using R2API;
using RiskOfChaos.Components;
using RiskOfChaos.ModifierController.DamageInfo;
using RiskOfChaos.ModifierController.Gravity;
using RiskOfChaos.ModifierController.Knockback;
using RiskOfChaos.ModifierController.Projectile;
using RiskOfChaos.ModifierController.SkillSlots;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Networking.Components;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos
{
    public static class NetPrefabs
    {
        public static GameObject GenericTeamInventoryPrefab { get; private set; }

        public static GameObject MonsterItemStealControllerPrefab { get; private set; }

        public static GameObject GravityControllerPrefab { get; private set; }

        public static GameObject SkillSlotModificationControllerPrefab { get; private set; }

        public static GameObject KnockbackModificationControllerPrefab { get; private set; }

        public static GameObject ProjectileModificationControllerPrefab { get; private set; }

        public static GameObject TimeScaleModificationControllerPrefab { get; private set; }

        public static GameObject DamageInfoModificationControllerPrefab { get; private set; }

        static GameObject createPrefabObject(string name, bool networked = true)
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
                GenericTeamInventoryPrefab = createPrefabObject("GenericTeamInventory");

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
            }

            // NetworkGravityControllerPrefab
            {
                GravityControllerPrefab = createPrefabObject("GravityController");

                GravityControllerPrefab.AddComponent<SetDontDestroyOnLoad>();
                GravityControllerPrefab.AddComponent<DestroyOnRunEnd>();
                GravityControllerPrefab.AddComponent<SyncWorldGravity>();
                GravityControllerPrefab.AddComponent<GravityModificationManager>();
            }

            // SkillSlotModificationControllerPrefab
            {
                SkillSlotModificationControllerPrefab = createPrefabObject("SkillSlotModificationController");

                SkillSlotModificationControllerPrefab.AddComponent<SetDontDestroyOnLoad>();
                SkillSlotModificationControllerPrefab.AddComponent<DestroyOnRunEnd>();
                SkillSlotModificationControllerPrefab.AddComponent<SkillSlotModificationManager>();
            }

            // KnockbackModificationControllerPrefab
            {
                KnockbackModificationControllerPrefab = createPrefabObject("KnockbackModificationController");

                KnockbackModificationControllerPrefab.AddComponent<SetDontDestroyOnLoad>();
                KnockbackModificationControllerPrefab.AddComponent<DestroyOnRunEnd>();
                KnockbackModificationControllerPrefab.AddComponent<KnockbackModificationManager>();
            }

            // ProjectileModificationControllerPrefab
            {
                ProjectileModificationControllerPrefab = createPrefabObject("ProjectileModificationController");

                ProjectileModificationControllerPrefab.AddComponent<SetDontDestroyOnLoad>();
                ProjectileModificationControllerPrefab.AddComponent<DestroyOnRunEnd>();
                ProjectileModificationControllerPrefab.AddComponent<ProjectileModificationManager>();
            }

            // TimeScaleModificationControllerPrefab
            {
                TimeScaleModificationControllerPrefab = createPrefabObject("TimeScaleModificationController");

                TimeScaleModificationControllerPrefab.AddComponent<SetDontDestroyOnLoad>();
                TimeScaleModificationControllerPrefab.AddComponent<DestroyOnRunEnd>();
                TimeScaleModificationControllerPrefab.AddComponent<SyncTimeScale>();
                TimeScaleModificationControllerPrefab.AddComponent<TimeScaleModificationManager>();
            }

            // DamageInfoModificationControllerPrefab
            {
                DamageInfoModificationControllerPrefab = createPrefabObject("DamageInfoModificationController", false);

                DamageInfoModificationControllerPrefab.AddComponent<SetDontDestroyOnLoad>();
                DamageInfoModificationControllerPrefab.AddComponent<DestroyOnRunEnd>();
                DamageInfoModificationControllerPrefab.AddComponent<DamageInfoModificationManager>();
            }

            Run.onRunStartGlobal += onRunStart;
        }

        static void onRunStart(Run _)
        {
            if (!NetworkServer.active)
                return;

            NetworkServer.Spawn(GameObject.Instantiate(GravityControllerPrefab));
            NetworkServer.Spawn(GameObject.Instantiate(SkillSlotModificationControllerPrefab));
            NetworkServer.Spawn(GameObject.Instantiate(KnockbackModificationControllerPrefab));
            NetworkServer.Spawn(GameObject.Instantiate(ProjectileModificationControllerPrefab));
            NetworkServer.Spawn(GameObject.Instantiate(TimeScaleModificationControllerPrefab));

            GameObject.Instantiate(DamageInfoModificationControllerPrefab);
        }
    }
}
