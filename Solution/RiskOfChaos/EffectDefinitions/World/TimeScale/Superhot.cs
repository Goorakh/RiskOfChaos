using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
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

        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                {
                    tryAddSuperhotController(body);
                }

                CharacterBody.onBodyStartGlobal += onBodyStartGlobal;
            }
        }

        void OnDestroy()
        {
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
            superhotController.OwnerEffectComponent = _effectComponent;

            if (superhotController.TryGetComponent(out IInterpolationProvider interpolationComponent))
            {
                interpolationComponent.SetInterpolationParameters(new InterpolationParameters(0.5f));
            }

            NetworkedBodyAttachment networkedBodyAttachment = superhotControllerObj.GetComponent<NetworkedBodyAttachment>();
            networkedBodyAttachment.AttachToGameObjectAndSpawn(body.gameObject);
        }
    }
}
