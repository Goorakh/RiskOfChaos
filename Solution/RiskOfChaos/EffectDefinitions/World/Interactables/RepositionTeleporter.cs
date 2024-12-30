using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosEffect("reposition_teleporter")]
    public sealed class RepositionTeleporter : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return TeleporterUtils.GetActiveTeleporterObjects().Count > 0;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            DirectorPlacementRule teleporterPlacementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            foreach (GameObject teleporterObject in TeleporterUtils.GetActiveTeleporterObjects())
            {
                if (teleporterObject)
                {
                    setTeleporterPosition(teleporterObject, teleporterPlacementRule.EvaluateToPosition(_rng));
                }
            }
        }

        [Server]
        void setTeleporterPosition(GameObject teleporterObject, Vector3 newPosition)
        {
            teleporterObject.transform.position = newPosition;
            RpcSetTeleporterPosition(teleporterObject, newPosition);
        }

        [ClientRpc]
        void RpcSetTeleporterPosition(GameObject teleporterObject, Vector3 newPosition)
        {
            if (teleporterObject)
            {
                teleporterObject.transform.position = newPosition;
            }
        }
    }
}
