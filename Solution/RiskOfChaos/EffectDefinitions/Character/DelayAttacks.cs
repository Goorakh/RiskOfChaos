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
    [ChaosTimedEffect("delay_attacks", 90f)]
    public sealed class DelayAttacks : NetworkBehaviour
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
        AttackDelayModificationProvider _attackDelayModificationProvider;

        void Start()
        {
            if (NetworkServer.active)
            {
                _attackDelayModificationController = GameObject.Instantiate(RoCContent.NetworkedPrefabs.AttackDelayModificationProvider).GetComponent<ValueModificationController>();

                _attackDelayModificationProvider = _attackDelayModificationController.GetComponent<AttackDelayModificationProvider>();
                updateAttackDelay();

                NetworkServer.Spawn(_attackDelayModificationController.gameObject);

                _attackDelay.SettingChanged += onAttackDelayChanged;
            }
        }

        void OnDestroy()
        {
            if (_attackDelayModificationController)
            {
                _attackDelayModificationController.Retire();
                _attackDelayModificationController = null;
                _attackDelayModificationProvider = null;
            }

            _attackDelay.SettingChanged -= onAttackDelayChanged;
        }

        void onAttackDelayChanged(object sender, ConfigChangedArgs<float> e)
        {
            updateAttackDelay();
        }

        [Server]
        void updateAttackDelay()
        {
            if (_attackDelayModificationProvider)
            {
                _attackDelayModificationProvider.Delay = _attackDelay.Value;
            }
        }
    }
}
