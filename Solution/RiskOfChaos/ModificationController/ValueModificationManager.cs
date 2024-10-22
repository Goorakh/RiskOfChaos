using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.ModificationController.AttackDelay;
using RiskOfChaos.ModificationController.Camera;
using RiskOfChaos.ModificationController.Cost;
using RiskOfChaos.ModificationController.Effect;
using RiskOfChaos.ModificationController.Gravity;
using RiskOfChaos.ModificationController.HoldoutZone;
using RiskOfChaos.ModificationController.Knockback;
using RiskOfChaos.ModificationController.Pickups;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.ModificationController.TimeScale;
using RiskOfChaos.ModificationController.UI;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.ModificationController
{
    public sealed class ValueModificationManager : MonoBehaviour
    {
        static ValueModificationManager _instance;
        public static ValueModificationManager Instance => _instance;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.ValueModificationManager), [
                typeof(SetDontDestroyOnLoad),
                typeof(AutoCreateOnRunStart),
                typeof(DestroyOnRunEnd),
                typeof(ValueModificationManager),
                typeof(AttackDelayModificationManager),
                typeof(CameraModificationManager),
                typeof(CostModificationManager),
                typeof(EffectModificationManager),
                typeof(GravityModificationManager),
                typeof(HoldoutZoneModificationManager),
                typeof(KnockbackModificationManager),
                typeof(PickupModificationManager),
                typeof(ProjectileModificationManager),
                typeof(SkillSlotModificationManager),
                typeof(TimeScaleModificationManager),
                typeof(UIModificationManager),
            ]);

            networkPrefabs.Add(prefab);
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }
    }
}
