using EntityStates;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("freeze_all")]
    public sealed class FreezeAll : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _freezeDuration =
            ConfigFactory<float>.CreateConfig("Freeze Duration", 2.5f)
                                .Description("How long all characters will be frozen for, in seconds")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f, FormatString = "{0}s" })
                                .Build();

        void Start()
        {
            CharacterBody.readOnlyInstancesList.TryDo(tryFreezeBody, FormatUtils.GetBestBodyName);
        }

        static void tryFreezeBody(CharacterBody body)
        {
            if (!body || !body.hasEffectiveAuthority)
                return;

            EntityStateMachine freezeStateMachine = null;
            EntityStateMachine[] idleStateMachines = [];

            if (body.TryGetComponent(out SetStateOnHurt setStateOnHurt))
            {
                freezeStateMachine = setStateOnHurt.targetStateMachine;
                idleStateMachines = setStateOnHurt.idleStateMachine;
            }
            else
            {
                EntityStateMachine bodyStateMachine = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
                if (bodyStateMachine)
                {
                    Log.Debug($"Freezing {FormatUtils.GetBestBodyName(body)} through StateMachine(s) directly");

                    freezeStateMachine = bodyStateMachine;

                    EntityStateMachine weaponStateMachine = EntityStateMachine.FindByCustomName(body.gameObject, "Weapon");
                    if (weaponStateMachine)
                    {
                        idleStateMachines = [weaponStateMachine];
                    }
                }
            }

            if (!freezeStateMachine)
                return;

            freezeStateMachine.SetNextState(new CustomFrozenState
            {
                freezeDuration = _freezeDuration.Value
            });

            foreach (EntityStateMachine stateMachine in idleStateMachines)
            {
                if (stateMachine)
                {
                    stateMachine.SetNextState(new Idle());
                }
            }
        }

        [EntityStateType]
        class CustomFrozenState : FrozenState
        {
            WormBodyPositions2 _wormBodyPositions;
            bool _wormBodyPositionsEnabled;

            WormBodyPositionsDriver _wormBodyPositionsDriver;
            bool _wormBodyPositionsDriverEnabled;

            public override void OnEnter()
            {
                base.OnEnter();

                _wormBodyPositions = GetComponent<WormBodyPositions2>();
                if (_wormBodyPositions)
                {
                    _wormBodyPositionsEnabled = _wormBodyPositions.enabled;
                    _wormBodyPositions.enabled = false;
                }

                _wormBodyPositionsDriver = GetComponent<WormBodyPositionsDriver>();
                if (_wormBodyPositionsDriver)
                {
                    _wormBodyPositionsDriverEnabled = _wormBodyPositionsDriver.enabled;
                    _wormBodyPositionsDriver.enabled = false;
                }
            }

            public override void OnExit()
            {
                if (_wormBodyPositions)
                {
                    _wormBodyPositions.enabled = _wormBodyPositionsEnabled;
                }

                if (_wormBodyPositionsDriver)
                {
                    _wormBodyPositionsDriver.enabled = _wormBodyPositionsDriverEnabled;
                }

                base.OnExit();
            }
        }
    }
}
