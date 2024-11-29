using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
    [ChaosTimedEffect("increase_gravity", TimedEffectType.UntilStageEnd, ConfigName = "Increase Gravity")]
    public sealed class IncreaseGravity : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _gravityIncrease =
            ConfigFactory<float>.CreateConfig("Increase per Activation", 0.5f)
                                .Description("How much gravity should increase per effect activation, 50% means the gravity is multiplied by 1.5, 100% means the gravity is multiplied by 2, etc.")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig
                                {
                                    FormatString = "+{0:P0}",
                                    Min = 0f
                                })
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_gravityIncrease) { ValueFormat = "P0" };
        }

        ValueModificationController _gravityModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _gravityModificationController = Instantiate(RoCContent.NetworkedPrefabs.GravityModificationProvider).GetComponent<ValueModificationController>();

                GravityModificationProvider gravityModificationProvider = _gravityModificationController.GetComponent<GravityModificationProvider>();
                gravityModificationProvider.GravityMultiplierConfigBinding.BindToConfig(_gravityIncrease, v => 1f + v);

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
