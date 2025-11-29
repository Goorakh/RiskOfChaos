using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("attract_pickups", 90f, AllowDuplicates = false)]
    [RequiredComponents(typeof(GenericAttractPickupsEffect))]
    public sealed class AttractPickups : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _speedMultiplier =
            ConfigFactory<float>.CreateConfig("Attract Strength Multiplier", 1f)
                                .Description("Multiplies the strength of the effect")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f, FormatString = "{0}x" })
                                .Build();

        ChaosEffectComponent _effectComponent;

        GenericAttractPickupsEffect _attractPickupsEffect;

        ItemTierPickupRulesOverride _itemPickupRulesOverride;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();

            _attractPickupsEffect = GetComponent<GenericAttractPickupsEffect>();
            _attractPickupsEffect.SetupAttractComponent += updateAttractComponent;
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _speedMultiplier.SettingChanged += onSpeedMultiplierChanged;
            }

            _itemPickupRulesOverride = new ItemTierPickupRulesOverride(ItemTierDef.PickupRules.Default);
        }

        void OnDestroy()
        {
            _attractPickupsEffect.SetupAttractComponent -= updateAttractComponent;

            _speedMultiplier.SettingChanged -= onSpeedMultiplierChanged;

            _itemPickupRulesOverride?.Dispose();
        }

        static void updateAttractComponent(AttractToPlayers attractComponent)
        {
            attractComponent.MaxDistance = 20f + (Mathf.Max(0f, _speedMultiplier.Value - 1f) * 10f);
            attractComponent.Acceleration = 25f * _speedMultiplier.Value;
            attractComponent.DynamicFrictionOverride = 0.1f;
        }

        void updateAllAttractComponents()
        {
            foreach (AttractToPlayers attractComponent in InstanceTracker.GetInstancesList<AttractToPlayers>())
            {
                if (attractComponent.OwnerEffectComponent == _effectComponent)
                {
                    updateAttractComponent(attractComponent);
                }
            }
        }

        void onSpeedMultiplierChanged(object sender, ConfigChangedArgs<float> e)
        {
            updateAllAttractComponents();
        }
    }
}
