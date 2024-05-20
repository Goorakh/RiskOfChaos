using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("give_tonic_affliction", DefaultSelectionWeight = 0.4f)]
    public sealed class GiveTonicAffliction : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _itemCount =
            ConfigFactory<int>.CreateConfig("Tonic Affliction Count", 1)
                              .Description("The amount of Tonic Affliction to give each player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
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
