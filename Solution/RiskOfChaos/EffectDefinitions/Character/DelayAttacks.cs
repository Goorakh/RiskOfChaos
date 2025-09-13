using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.AttackDelay;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    /*
    [ChaosTimedEffect("delay_attacks", 90f)]
    public sealed class DelayAttacks : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _attackDelay =
            ConfigFactory<float>.CreateConfig("Attack Delay", 0.5f)
                                .Description("The delay to apply to all attacks")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f, FormatString = "{0}s" })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.AttackDelayModificationProvider;
        }

        ValueModificationController _attackDelayModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _attackDelayModificationController = GameObject.Instantiate(RoCContent.NetworkedPrefabs.AttackDelayModificationProvider).GetComponent<ValueModificationController>();

                AttackDelayModificationProvider attackDelayModificationProvider = _attackDelayModificationController.GetComponent<AttackDelayModificationProvider>();
                attackDelayModificationProvider.DelayConfigBinding.BindToConfig(_attackDelay);

                NetworkServer.Spawn(_attackDelayModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_attackDelayModificationController)
            {
                _attackDelayModificationController.Retire();
                _attackDelayModificationController = null;
            }
        }
    }
    */
}
