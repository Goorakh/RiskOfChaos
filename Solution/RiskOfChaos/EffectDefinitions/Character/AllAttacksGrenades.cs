using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.ContentManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("all_attacks_grenades", 90f, AllowDuplicates = false)]
    public sealed class AllAttacksGrenades : MonoBehaviour
    {
        [PrefabInitializer]
        static IEnumerator InitPrefab(GameObject prefab)
        {
            AsyncOperationHandle<GameObject> commandoBodyLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Commando_CommandoBody_prefab, AsyncReferenceHandleUnloadType.Preload);
            yield return commandoBodyLoad;

            GameObject commandoBodyPrefab = commandoBodyLoad.Result;
            if (commandoBodyPrefab && commandoBodyPrefab.TryGetComponent(out AkBank commandoBank) && commandoBank.data?.ObjectReference)
            {
                AkBank effectBank = prefab.AddComponent<AkBank>();
                effectBank.data = commandoBank.data;
                effectBank.triggerList = [AkTriggerHandler.START_TRIGGER_ID];
                effectBank.unloadTriggerList = [AkTriggerHandler.DESTROY_TRIGGER_ID];
            }
            else
            {
                Log.Error("Failed to find commando sound bank");
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.ProjectileModificationProvider;
        }

        ValueModificationController _projectileModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _projectileModificationController = Instantiate(RoCContent.NetworkedPrefabs.ProjectileModificationProvider).GetComponent<ValueModificationController>();

                ProjectileModificationProvider projectileModificationProvider = _projectileModificationController.GetComponent<ProjectileModificationProvider>();

                projectileModificationProvider.OverrideProjectileIndex = ProjectileCatalog.GetProjectileIndex(RoCContent.ProjectilePrefabs.GrenadeReplacedProjectile);

                NetworkServer.Spawn(_projectileModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_projectileModificationController)
            {
                _projectileModificationController.Retire();
                _projectileModificationController = null;
            }
        }
    }
}
