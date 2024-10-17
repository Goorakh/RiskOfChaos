using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Cost;
using RiskOfOptions.OptionConfigs;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosTimedEffect("increase_interactable_costs", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.8f, ConfigName = "Increase Chest Prices")]
    public sealed class IncreaseInteractableCosts : NetworkBehaviour
    {
        const float INCREASE_AMOUNT_MIN_VALUE = 0.05f;

        [EffectConfig]
        static readonly ConfigHolder<float> _increaseAmount =
            ConfigFactory<float>.CreateConfig("Increase Amount", 0.25f)
                                .Description("The amount to increase costs by")
                                .AcceptableValues(new AcceptableValueMin<float>(INCREASE_AMOUNT_MIN_VALUE))
                                .OptionConfig(new FloatFieldConfig { FormatString = "+{0:P0}", Min = INCREASE_AMOUNT_MIN_VALUE })
                                .FormatsEffectName()
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.CostModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_increaseAmount.Value) { ValueFormat = "P0" };
        }

        ValueModificationController _costModificationController;
        CostModificationProvider _costModificationProvider;

        void Start()
        {
            if (NetworkServer.active)
            {
                _costModificationController = Instantiate(RoCContent.NetworkedPrefabs.CostModificationProvider).GetComponent<ValueModificationController>();

                _costModificationProvider = _costModificationController.GetComponent<CostModificationProvider>();
                refreshCostMultiplier();

                _increaseAmount.SettingChanged += onIncreaseAmountChanged;

                NetworkServer.Spawn(_costModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_costModificationController)
            {
                _costModificationController.Retire();
                _costModificationController = null;
                _costModificationProvider = null;
            }

            _increaseAmount.SettingChanged -= onIncreaseAmountChanged;
        }

        void onIncreaseAmountChanged(object sender, ConfigChangedArgs<float> e)
        {
            refreshCostMultiplier();
        }

        void refreshCostMultiplier()
        {
            if (_costModificationProvider)
            {
                _costModificationProvider.CostMultiplier = 1f + _increaseAmount.Value;
            }
        }
    }
}
