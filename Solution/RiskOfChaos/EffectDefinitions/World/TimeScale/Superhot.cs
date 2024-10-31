using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    [ChaosTimedEffect("superhot", 45f, AllowDuplicates = false)]
    public sealed class Superhot : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkedPrefabs)
        {
            // SuperhotController
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.SuperhotController), [
                    typeof(NetworkedBodyAttachment),
                    typeof(NetworkedInterpolationComponent),
                    typeof(SuperhotPlayerController)
                ]);

                NetworkIdentity networkIdentity = prefab.GetComponent<NetworkIdentity>();
                networkIdentity.localPlayerAuthority = true;

                NetworkedBodyAttachment networkedBodyAttachment = prefab.GetComponent<NetworkedBodyAttachment>();
                networkedBodyAttachment.shouldParentToAttachedBody = true;
                networkedBodyAttachment.forceHostAuthority = false;

                networkedPrefabs.Add(prefab);
            }
        }

        readonly HashSet<SuperhotPlayerController> _superhotControllers = [];

        void Start()
        {
            if (NetworkServer.active)
            {
                _superhotControllers.EnsureCapacity(PlayerCharacterMasterController.instances.Count);
                foreach (PlayerCharacterMasterController playerMasterController in PlayerCharacterMasterController.instances)
                {
                    CharacterMaster master = playerMasterController.master;
                    if (!master)
                        continue;

                    CharacterBody body = master.GetBody();
                    if (!body)
                        continue;

                    createSuperhotController(body);
                }

                CharacterBody.onBodyStartGlobal += onBodyStartGlobal;
            }
        }

        void OnDestroy()
        {
            foreach (SuperhotPlayerController superhotController in _superhotControllers)
            {
                if (superhotController)
                {
                    superhotController.Retire();
                }
            }

            _superhotControllers.Clear();

            CharacterBody.onBodyStartGlobal -= onBodyStartGlobal;
        }

        void onBodyStartGlobal(CharacterBody body)
        {
            if (body.isPlayerControlled)
            {
                createSuperhotController(body);
            }
        }

        void createSuperhotController(CharacterBody body)
        {
            GameObject superhotControllerObj = GameObject.Instantiate(RoCContent.NetworkedPrefabs.SuperhotController);

            SuperhotPlayerController superhotController = superhotControllerObj.GetComponent<SuperhotPlayerController>();

            if (superhotController.TryGetComponent(out IInterpolationProvider interpolationComponent))
            {
                interpolationComponent.SetInterpolationParameters(new InterpolationParameters(0.5f));
            }

            _superhotControllers.Add(superhotController);

            NetworkedBodyAttachment networkedBodyAttachment = superhotControllerObj.GetComponent<NetworkedBodyAttachment>();
            networkedBodyAttachment.AttachToGameObjectAndSpawn(body.gameObject);
        }
    }
}
