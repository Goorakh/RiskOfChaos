using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Networking.Components;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    [ChaosTimedEffect("superhot", 45f, AllowDuplicates = false)]
    public sealed class Superhot : TimedEffect
    {
        readonly HashSet<SuperhotPlayerController> _superhotControllers = [];

        public override void OnStart()
        {
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

            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        public override void OnEnd()
        {
            foreach (SuperhotPlayerController superhotController in _superhotControllers)
            {
                if (superhotController)
                {
                    NetworkServer.Destroy(superhotController.gameObject);
                }
            }

            _superhotControllers.Clear();

            CharacterBody.onBodyStartGlobal -= CharacterBody_onBodyStartGlobal;
        }

        void CharacterBody_onBodyStartGlobal(CharacterBody body)
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
            _superhotControllers.Add(superhotController);

            NetworkedBodyAttachment networkedBodyAttachment = superhotControllerObj.GetComponent<NetworkedBodyAttachment>();
            networkedBodyAttachment.AttachToGameObjectAndSpawn(body.gameObject);
        }
    }
}
