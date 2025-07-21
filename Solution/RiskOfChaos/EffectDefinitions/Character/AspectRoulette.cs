using HarmonyLib;
using HG;
using Newtonsoft.Json;
using RiskOfChaos.Collections;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("aspect_roulette", 60f, AllowDuplicates = false)]
    public sealed class AspectRoulette : NetworkBehaviour
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDirectorUnavailableElites =
            ConfigFactory<bool>.CreateConfig("Ignore Elite Selection Rules", false)
                               .Description("If the effect should ignore normal elite selection rules. If enabled, any elite type can be selected, if disabled, only the elite types that can currently be spawned on the stage can be selected")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();


        readonly struct AspectConfig
        {
            public readonly EliteDef EliteDef;

            public readonly ConfigHolder<float> WeightConfig;

            public AspectConfig(EliteDef eliteDef)
            {
                EliteDef = eliteDef;

                Language language = Language.english;
                string equipmentName = language.GetLocalizedStringByToken(eliteDef.eliteEquipmentDef.nameToken);
                if (string.IsNullOrWhiteSpace(equipmentName))
                {
                    equipmentName = eliteDef.eliteEquipmentDef.name;
                }

                if (string.IsNullOrWhiteSpace(equipmentName))
                {
                    equipmentName = eliteDef.eliteEquipmentDef.equipmentIndex.ToString();
                }

                string eliteName = language.GetLocalizedFormattedStringByToken(eliteDef.modifierToken, string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(eliteName))
                {
                    eliteName = eliteDef.name;
                }

                if (string.IsNullOrWhiteSpace(eliteName))
                {
                    eliteName = eliteDef.eliteIndex.ToString();
                }

                string combinedEliteName = $"{equipmentName} ({eliteName})".Trim();

                WeightConfig =
                    ConfigFactory<float>.CreateConfig($"{combinedEliteName} Weight", 1f)
                                        .Description($"Controls how likely the {eliteName.ToLower()} elite aspect is during the effect, set to 0 to exclude it from the effect")
                                        .AcceptableValues(new AcceptableValueMin<float>(0f))
                                        .OptionConfig(new FloatFieldConfig { Min = 0f })
                                        .Build();
            }

            public readonly void Bind(ChaosEffectInfo effectInfo)
            {
                WeightConfig.Bind(effectInfo);
            }
        }

        static AspectConfig[] _aspectConfigs = [];

        static float getAspectWeight(EliteIndex eliteIndex)
        {
            if (ArrayUtils.IsInBounds(_aspectConfigs, (int)eliteIndex))
            {
                AspectConfig aspectConfig = _aspectConfigs[(int)eliteIndex];
                if (aspectConfig.WeightConfig is not null)
                {
                    return aspectConfig.WeightConfig.Value;
                }
            }

            return 0f;
        }

        [SystemInitializer(typeof(ChaosEffectCatalog), typeof(EliteCatalog), typeof(EquipmentCatalog))]
        static void Init()
        {
            _aspectConfigs = new AspectConfig[EliteCatalog.eliteList.Count];
            for (int i = 0; i < _aspectConfigs.Length; i++)
            {
                EliteIndex eliteIndex = (EliteIndex)i;
                if (!EliteUtils.IsAvailable(eliteIndex))
                    continue;

                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (!eliteDef || !eliteDef.eliteEquipmentDef)
                    continue;

                _aspectConfigs[i] = new AspectConfig(eliteDef);
            }

            foreach (AspectConfig config in _aspectConfigs.Where(c => c.EliteDef && c.WeightConfig is not null)
                                                          .OrderBy(c => Language.GetString(c.EliteDef.modifierToken, "en")))
            {
                config.Bind(_effectInfo);
            }
        }

        [Serializable]
        class AspectStep
        {
            [JsonProperty("a")]
            public EquipmentIndex AspectEquipmentIndex { get; set; }

            [JsonProperty("d")]
            public float Duration { get; set; }

            public AspectStep(EquipmentIndex aspectEquipmentIndex, float duration)
            {
                AspectEquipmentIndex = aspectEquipmentIndex;
                Duration = duration;
            }
        }

        static AspectStep generateStep(Xoroshiro128Plus rng)
        {
            IReadOnlyList<EliteIndex> elites = EliteUtils.GetRunAvailableElites(_allowDirectorUnavailableElites.Value);

            WeightedSelection<EquipmentIndex> eliteEquipmentSelector = new WeightedSelection<EquipmentIndex>();
            eliteEquipmentSelector.EnsureCapacity(elites.Count);
            foreach (EliteIndex eliteIndex in elites)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                eliteEquipmentSelector.AddChoice(eliteDef.eliteEquipmentDef.equipmentIndex, getAspectWeight(eliteIndex));
            }

            EquipmentIndex aspectEquipmentIndex = eliteEquipmentSelector.Evaluate(rng.nextNormalizedFloat);
            float aspectDuration = rng.RangeFloat(MIN_ASPECT_DURATION, MAX_ASPECT_DURATION);
            return new AspectStep(aspectEquipmentIndex, aspectDuration);
        }

        const float MIN_ASPECT_DURATION = 1f;
        const float MAX_ASPECT_DURATION = 7.5f;

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _aspectStepRng;

        AspectStep[] _playerAspectSteps;
        float _totalAspectStepsDuration;

        readonly ClearingObjectList<RandomlySwapAspect> _swapAspectComponents = [];

        [SerializedMember("s")]
        AspectStep[] serializedPlayerAspectSteps
        {
            get => _playerAspectSteps;
            set
            {
                _playerAspectSteps = value;

                _totalAspectStepsDuration = 0f;

                if (_playerAspectSteps != null)
                {
                    foreach (AspectStep step in _playerAspectSteps)
                    {
                        _totalAspectStepsDuration += step.Duration;
                    }
                }
            }
        }

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _aspectStepRng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _totalAspectStepsDuration = 0f;

            // Generate as many steps as needed for the fixed duration,
            // otherwise create a cycle long enough that the looping would hopefully not be noticeable
            float effectDuration = 120f;
            if (_effectComponent.DurationComponent)
            {
                if (_effectComponent.DurationComponent.TimedType == TimedEffectType.FixedDuration)
                {
                    effectDuration = _effectComponent.DurationComponent.Duration;
                }
            }

            List<AspectStep> aspectSteps = new List<AspectStep>(Mathf.CeilToInt(effectDuration / MIN_ASPECT_DURATION));

            while (effectDuration > 0f)
            {
                AspectStep step = generateStep(_aspectStepRng);

                bool isDuplicateStep = false;

                // Save a tiny bit of space by collapsing together neighboring steps with the same aspect
                if (aspectSteps.Count > 0)
                {
                    AspectStep previousStep = aspectSteps[aspectSteps.Count - 1];
                    if (previousStep.AspectEquipmentIndex == step.AspectEquipmentIndex)
                    {
                        aspectSteps[aspectSteps.Count - 1] = new AspectStep(previousStep.AspectEquipmentIndex, previousStep.Duration + step.Duration);
                        isDuplicateStep = true;
                    }
                }

                if (!isDuplicateStep)
                {
                    aspectSteps.Add(step);
                }

                effectDuration -= step.Duration;
                _totalAspectStepsDuration += step.Duration;
            }

            _playerAspectSteps = [.. aspectSteps];
        }

        AspectStep getCurrentAspectStep(CharacterBody body)
        {
            if (_playerAspectSteps != null && body && body.isPlayerControlled)
            {
                float time = _effectComponent.TimeStarted.TimeSinceClamped % _totalAspectStepsDuration;
                foreach (AspectStep step in _playerAspectSteps)
                {
                    if (time < step.Duration)
                    {
                        return new AspectStep(step.AspectEquipmentIndex, step.Duration - time);
                    }

                    time -= step.Duration;
                }

                Log.Error($"Effect time out of bounds for {FormatUtils.GetBestBodyName(body)}");
            }

            return generateStep(_aspectStepRng);
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _swapAspectComponents.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);

                CharacterBody.readOnlyInstancesList.Do(tryAddComponentToBody);

                CharacterBody.onBodyStartGlobal += tryAddComponentToBody;
            }
        }

        void OnDestroy()
        {
            CharacterBody.onBodyStartGlobal -= tryAddComponentToBody;

            _swapAspectComponents.ClearAndDispose(true);
        }

        void tryAddComponentToBody(CharacterBody body)
        {
            try
            {
                RandomlySwapAspect randomlySwapAspect = body.gameObject.AddComponent<RandomlySwapAspect>();
                randomlySwapAspect.EffectInstance = this;

                _swapAspectComponents.Add(randomlySwapAspect);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to add component to {Util.GetBestBodyName(body.gameObject)}: {ex}");
            }
        }

        [ClientRpc]
        void RpcOnAspectSwitched(GameObject masterObject)
        {
            if (masterObject && masterObject.TryGetComponent(out CharacterMaster master))
            {
                GameObject bodyObject = master.GetBodyObject();
                if (bodyObject)
                {
                    Util.PlaySound("Play_UI_item_pickup", bodyObject);
                }
            }
        }

        [RequireComponent(typeof(CharacterBody))]
        class RandomlySwapAspect : MonoBehaviour
        {
            public AspectRoulette EffectInstance;
            CharacterBody _body;

            float _aspectReplaceTimer;

            void Awake()
            {
                _body = GetComponent<CharacterBody>();
            }

            void FixedUpdate()
            {
                _aspectReplaceTimer -= Time.fixedDeltaTime;
                if (_aspectReplaceTimer <= 0f)
                {
                    if (!_body || !EffectInstance)
                    {
                        Destroy(this);
                        return;
                    }

                    AspectStep currentStep = EffectInstance.getCurrentAspectStep(_body);

                    tryReplaceAspect(currentStep.AspectEquipmentIndex);
                    _aspectReplaceTimer = currentStep.Duration;
                }
            }

            void tryReplaceAspect(EquipmentIndex aspectEquipment)
            {
                if (!_body)
                    return;

                Inventory inventory = _body.inventory;
                if (!inventory)
                    return;

                EquipmentIndex currentEquipment = inventory.GetEquipmentIndex();
                if (currentEquipment == aspectEquipment)
                    return;

                if (currentEquipment != EquipmentIndex.None && !EliteUtils.IsEliteEquipment(currentEquipment))
                {
                    if (_body.isPlayerControlled)
                        return;

                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(currentEquipment), _body.corePosition, Vector3.up * 15f);
                }

                inventory.SetEquipmentIndex(aspectEquipment);

                GameObject masterObject = _body.masterObject;
                if (masterObject)
                {
                    EffectInstance.RpcOnAspectSwitched(masterObject);
                }
            }
        }
    }
}
