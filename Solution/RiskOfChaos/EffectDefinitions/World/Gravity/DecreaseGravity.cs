using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Gravity;
using RiskOfOptions.OptionConfigs;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Gravity
{
    [ChaosTimedEffect("decrease_gravity", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Gravity")]
    public sealed class DecreaseGravity : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _gravityDecrease =
            ConfigFactory<float>.CreateConfig("Decrease per Activation", 0.5f)
                                .Description("How much gravity should decrease per effect activation, 50% means the gravity is multiplied by 0.5, 100% means the gravity is reduced to 0, 0% means gravity doesn't change at all. etc.")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f,
                                    FormatString = "-{0:P0}"
                                })
                                .FormatsEffectName()
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_gravityDecrease.Value) { ValueFormat = "P0" };
        }

        ValueModificationController _gravityModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _gravityModificationController = Instantiate(RoCContent.NetworkedPrefabs.GravityModificationProvider).GetComponent<ValueModificationController>();

                GravityModificationProvider gravityModificationProvider = _gravityModificationController.GetComponent<GravityModificationProvider>();
                gravityModificationProvider.GravityMultiplierConfigBinding.BindToConfig(_gravityDecrease, v => 1f - v);

                NetworkServer.Spawn(_gravityModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_gravityModificationController)
            {
                _gravityModificationController.Retire();
                _gravityModificationController = null;
            }
        }
    }
}
