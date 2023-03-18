using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_tonic_affliction", DefaultSelectionWeight = 0.4f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 40f)]
    public sealed class GiveTonicAffliction : BaseEffect
    {
        public override void OnStart()
        {
            foreach (CharacterMaster master in PlayerUtils.GetAllPlayerMasters(false))
            {
                master.inventory.GiveItem(RoR2Content.Items.TonicAffliction);
            }
        }
    }
}
