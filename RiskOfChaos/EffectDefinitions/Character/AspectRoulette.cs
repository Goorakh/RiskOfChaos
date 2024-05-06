using HarmonyLib;
using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
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
    public sealed class AspectRoulette : TimedEffect
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

                Language language = Language.FindLanguageByName("en");
                string equipmentName = language.GetLocalizedStringByToken(eliteDef.eliteEquipmentDef.nameToken);
                string eliteName = language.GetLocalizedFormattedStringByToken(eliteDef.modifierToken, string.Empty).Trim();

                string combinedEliteName = $"{equipmentName} ({eliteName})";

                WeightConfig =
                    ConfigFactory<float>.CreateConfig($"{combinedEliteName} Weight", 1f)
                                        .Description($"Controls how likely the {eliteName.ToLower()} elite aspect is during the effect")
                                        .AcceptableValues(new AcceptableValueMin<float>(0f))
                                        .OptionConfig(new StepSliderConfig
                                        {
                                            min = 0f,
                                            max = 2.5f,
                                            increment = 0.05f
                                        })
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

        [SystemInitializer(typeof(ChaosEffectCatalog), typeof(EliteCatalog))]
        static void Init()
        {
            _aspectConfigs = new AspectConfig[EliteCatalog.eliteList.Count];
            for (int i = 0; i < _aspectConfigs.Length; i++)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef((EliteIndex)i);
                if (eliteDef.name.EndsWith("Honor"))
                    continue;

                if (Language.IsTokenInvalid(eliteDef.modifierToken) || Language.IsTokenInvalid(eliteDef.eliteEquipmentDef.nameToken))
                    continue;

                _aspectConfigs[i] = new AspectConfig(eliteDef);
            }

            foreach (AspectConfig config in _aspectConfigs.Where(c => c.EliteDef && c.WeightConfig is not null)
                                                          .OrderBy(c => Language.GetString(c.EliteDef.modifierToken, "en")))
            {
                config.Bind(_effectInfo);
            }
        }

        bool _isSeeded;

        readonly record struct AspectStep(EquipmentIndex AspectEquipmentIndex, float Duration);
        AspectStep[] _playerAspectSteps;
        float _totalAspectStepsDuration;

        const float MIN_ASPECT_DURATION = 1f;
        const float MAX_ASPECT_DURATION = 7.5f;

        static AspectStep generateStep(Xoroshiro128Plus rng)
        {
            EliteIndex[] elites = EliteUtils.GetElites(_allowDirectorUnavailableElites.Value);

            WeightedSelection<EquipmentIndex> eliteEquipmentSelector = new WeightedSelection<EquipmentIndex>(elites.Length);
            foreach (EliteIndex eliteIndex in elites)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                eliteEquipmentSelector.AddChoice(eliteDef.eliteEquipmentDef.equipmentIndex, getAspectWeight(eliteIndex));
            }

            EquipmentIndex aspectEquipmentIndex = eliteEquipmentSelector.Evaluate(rng.nextNormalizedFloat);
            float aspectDuration = rng.RangeFloat(MIN_ASPECT_DURATION, MAX_ASPECT_DURATION);
            return new AspectStep(aspectEquipmentIndex, aspectDuration);
        }

        AspectStep getCurrentAspectStep(CharacterBody body)
        {
            if (_isSeeded && body && body.isPlayerControlled)
            {
                float time = TimeElapsed % _totalAspectStepsDuration;
                foreach (AspectStep step in _playerAspectSteps)
                {
                    time -= step.Duration;
                    if (time < 0f)
                    {
                        return new AspectStep(step.AspectEquipmentIndex, -time);
                    }
                }

                Log.Error($"Effect time out of bounds for {FormatUtils.GetBestBodyName(body)}");
            }

            return generateStep(RoR2Application.rng);
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _isSeeded = Configs.EffectSelection.SeededEffectSelection.Value;
            if (_isSeeded)
            {
                _totalAspectStepsDuration = 0f;

                // Generate as many steps as needed for the fixed duration, otherwise create a cycle long enough people probably won't notice the looping
                float time = TimedType == TimedEffectType.FixedDuration ? DurationSeconds : 120f;

                const float AVERAGE_ASPECT_DURATION = (MIN_ASPECT_DURATION + MAX_ASPECT_DURATION) / 2f;
                List<AspectStep> aspectSteps = new List<AspectStep>(Mathf.CeilToInt(time / AVERAGE_ASPECT_DURATION));

                while (time > 0f)
                {
                    AspectStep step = generateStep(RNG);
                    aspectSteps.Add(step);
                    time -= step.Duration;
                    _totalAspectStepsDuration += step.Duration;
                }

                // Save a tiny bit of space by collapsing together neighboring steps with the same aspect
                for (int i = aspectSteps.Count - 1; i >= 1; i--)
                {
                    AspectStep previousStep = aspectSteps[i - 1];
                    AspectStep currentStep = aspectSteps[i];

                    if (previousStep.AspectEquipmentIndex == currentStep.AspectEquipmentIndex)
                    {
                        aspectSteps[i - 1] = new AspectStep(previousStep.AspectEquipmentIndex, previousStep.Duration + currentStep.Duration);
                        aspectSteps.RemoveAt(i);
                    }
                }

                _playerAspectSteps = aspectSteps.ToArray();
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(_isSeeded);
            if (_isSeeded)
            {
                writer.WritePackedUInt32((uint)_playerAspectSteps.Length);
                foreach (AspectStep aspectStep in _playerAspectSteps)
                {
                    writer.Write(aspectStep.AspectEquipmentIndex);
                    writer.Write(aspectStep.Duration);
                }
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _isSeeded = reader.ReadBoolean();
            if (_isSeeded)
            {
                _totalAspectStepsDuration = 0f;

                _playerAspectSteps = new AspectStep[reader.ReadPackedUInt32()];
                for (int i = 0; i < _playerAspectSteps.Length; i++)
                {
                    EquipmentIndex aspectEquipmentIndex = reader.ReadEquipmentIndex();
                    float duration = reader.ReadSingle();

                    _playerAspectSteps[i] = new AspectStep(aspectEquipmentIndex, duration);
                    _totalAspectStepsDuration += duration;
                }
            }
        }

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.Do(tryAddComponentToBody);

            CharacterBody.onBodyStartGlobal += tryAddComponentToBody;
        }

        void tryAddComponentToBody(CharacterBody body)
        {
            try
            {
                RandomlySwapAspect randomlySwapAspect = body.gameObject.AddComponent<RandomlySwapAspect>();
                randomlySwapAspect.EffectInstance = this;
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to add component to {Util.GetBestBodyName(body.gameObject)}: {ex}");
            }
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= tryAddComponentToBody;

            InstanceUtils.DestroyAllTrackedInstances<RandomlySwapAspect>();
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

                InstanceTracker.Add(this);
            }

            void FixedUpdate()
            {
                _aspectReplaceTimer -= Time.fixedDeltaTime;
                if (_aspectReplaceTimer <= 0)
                {
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

                if (!_body.isPlayerControlled || currentEquipment == EquipmentIndex.None || EliteUtils.IsEliteEquipment(currentEquipment))
                {
                    inventory.SetEquipmentIndex(aspectEquipment);
                }
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);
            }
        }
    }
}
