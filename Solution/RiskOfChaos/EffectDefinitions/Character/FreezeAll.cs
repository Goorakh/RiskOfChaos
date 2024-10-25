using EntityStates;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
#if DEBUG
                    Log.Debug($"Freezing {FormatUtils.GetBestBodyName(body)} through StateMachine(s) directly");
#endif

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

            freezeStateMachine.SetNextState(new FrozenState
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
    }
}
