using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("reposition_teleporter", IsNetworked = true)]
    public sealed class RepositionTeleporter : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return TeleporterInteraction.instance;
        }

        Vector3 _newTeleporterPosition;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();
            _newTeleporterPosition = SpawnUtils.GetBestValidRandomPlacementRule().EvaluateToPosition(RNG);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_newTeleporterPosition);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _newTeleporterPosition = reader.ReadVector3();
        }

        public override void OnStart()
        {
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.instance.transform.position = _newTeleporterPosition;
            }
        }
    }
}
