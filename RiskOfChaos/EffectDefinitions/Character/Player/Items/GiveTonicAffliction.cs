using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_tonic_affliction", DefaultSelectionWeight = 0.4f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 40f)]
    public sealed class GiveTonicAffliction : BaseEffect
    {
        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
            {
                master.inventory.GiveItem(RoR2Content.Items.TonicAffliction);
                GenericPickupController.SendPickupMessage(master, PickupCatalog.FindPickupIndex(RoR2Content.Items.TonicAffliction.itemIndex));
            }, Util.GetBestMasterName);
        }
    }
}
