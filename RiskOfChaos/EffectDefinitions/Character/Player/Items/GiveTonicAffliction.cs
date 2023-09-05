using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_tonic_affliction", DefaultSelectionWeight = 0.4f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 40f)]
    public sealed class GiveTonicAffliction : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _itemCount =
            ConfigFactory<int>.CreateConfig("Tonic Affliction Count", 1)
                              .Description("The amount of Tonic Affliction to give each player")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 15
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
            {
                master.inventory.GiveItem(RoR2Content.Items.TonicAffliction, _itemCount.Value);
                GenericPickupController.SendPickupMessage(master, PickupCatalog.FindPickupIndex(RoR2Content.Items.TonicAffliction.itemIndex));
            }, Util.GetBestMasterName);
        }
    }
}
