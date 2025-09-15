using RiskOfChaos.Collections;
using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    [ChaosTimedEffect("superhot", 45f, AllowDuplicates = false)]
    public sealed class Superhot : MonoBehaviour
    {
        [ContentInitializer]
        static void LoadContent(ContentIntializerArgs args)
        {
            GameObject superhotController;
            {
                superhotController = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.SuperhotController), [
                    typeof(NetworkedBodyAttachment),
                    typeof(NetworkedInterpolationComponent),
                    typeof(SuperhotPlayerController)
                ]);

                NetworkIdentity networkIdentity = superhotController.GetComponent<NetworkIdentity>();
                networkIdentity.localPlayerAuthority = true;

                NetworkedBodyAttachment networkedBodyAttachment = superhotController.GetComponent<NetworkedBodyAttachment>();
                networkedBodyAttachment.shouldParentToAttachedBody = true;
                networkedBodyAttachment.forceHostAuthority = false;

            }

            args.ContentPack.networkedObjectPrefabs.Add([superhotController]);
        }

        readonly ClearingObjectList<SuperhotPlayerController> _superhotControllers = new ClearingObjectList<SuperhotPlayerController>()
        {
            DestroyComponentGameObject = true
        };

        void Start()
        {
            if (NetworkServer.active)
            {
                _superhotControllers.EnsureCapacity(PlayerCharacterMasterController.instances.Count);
                foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                {
                    tryAddSuperhotController(body);
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

            _superhotControllers.ClearAndDispose(false);

            CharacterBody.onBodyStartGlobal -= onBodyStartGlobal;
        }

        void onBodyStartGlobal(CharacterBody body)
        {
            tryAddSuperhotController(body);
        }

        void tryAddSuperhotController(CharacterBody body)
        {
            if (body.isPlayerControlled)
            {
                createSuperhotController(body);
            }
        }

        void createSuperhotController(CharacterBody body)
        {
            GameObject superhotControllerObj = Instantiate(RoCContent.NetworkedPrefabs.SuperhotController);

            SuperhotPlayerController superhotController = superhotControllerObj.GetComponent<SuperhotPlayerController>();

            if (superhotController.TryGetComponent(out IInterpolationProvider interpolationComponent))
            {
                interpolationComponent.SetInterpolationParameters(new InterpolationParameters(0.5f));
            }

            NetworkedBodyAttachment networkedBodyAttachment = superhotControllerObj.GetComponent<NetworkedBodyAttachment>();
            networkedBodyAttachment.AttachToGameObjectAndSpawn(body.gameObject);

            _superhotControllers.Add(superhotController);
        }
    }
}
