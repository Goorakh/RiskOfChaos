using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Cost;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosTimedEffect("decrease_interactable_costs", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.8f, ConfigName = "Decrease Chest Prices")]
    public sealed class DecreaseInteractableCosts : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _decreaseAmount =
            ConfigFactory<float>.CreateConfig("Decrease Amount", 0.25f)
                                .Description("The amount to decrease costs by")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.05f
                                })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.CostModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_decreaseAmount) { ValueFormat = "P0" };
        }

        ValueModificationController _costModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _costModificationController = Instantiate(RoCContent.NetworkedPrefabs.CostModificationProvider).GetComponent<ValueModificationController>();

                CostModificationProvider costModificationProvider = _costModificationController.GetComponent<CostModificationProvider>();
                costModificationProvider.CostMultiplierConfigBinding.BindToConfig(_decreaseAmount, v => 1f - v);

                NetworkServer.Spawn(_costModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_costModificationController)
            {
                _costModificationController.Retire();
                _costModificationController = null;
            }
        }
    }
}
