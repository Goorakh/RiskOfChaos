using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_random_elite_aspect", DefaultSelectionWeight = 0.6f)]
    public sealed class GiveRandomEliteAspect : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return EliteUtils.HasAnyAvailableEliteEquipments;
        }

        public override void OnStart()
        {
            PickupDef aspectPickupDef = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(EliteUtils.GetRandomEliteEquipmentIndex(RNG)));

            PlayerUtils.GetAllPlayerMasters(true).TryDo(playerMaster =>
            {
                PickupUtils.GrantOrDropPickupAt(aspectPickupDef, playerMaster);
            }, Util.GetBestMasterName);
        }
    }
}
