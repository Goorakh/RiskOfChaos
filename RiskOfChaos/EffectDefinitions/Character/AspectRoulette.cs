using HarmonyLib;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("aspect_roulette", 90f, AllowDuplicates = false)]
    public sealed class AspectRoulette : TimedEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDirectorUnavailableElites =
            ConfigFactory<bool>.CreateConfig("Ignore Elite Selection Rules", false)
                               .Description("If the effect should ignore normal elite selection rules. If enabled, any elite type can be selected, if disabled, only the elite types that can currently be spawned on the stage can be selected")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        bool _isSeeded;

        readonly record struct AspectStep(EquipmentIndex AspectEquipmentIndex, float Duration);
        AspectStep[] _playerAspectSteps;
        float _totalAspectStepsDuration;

        static AspectStep generateStep(Xoroshiro128Plus rng)
        {
            EquipmentIndex aspectEquipmentIndex = EliteUtils.SelectEliteEquipment(new Xoroshiro128Plus(rng.nextUlong), _allowDirectorUnavailableElites.Value);
            return new AspectStep(aspectEquipmentIndex, rng.RangeFloat(1f, 7.5f));
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

                List<AspectStep> aspectSteps = new List<AspectStep>();

                // Generate as many steps as needed for the fixed duration, otherwise create a cycle long enough people probably won't notice the looping
                float time = TimedType == TimedEffectType.FixedDuration ? DurationSeconds : 120f;
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
                    BossUtils.TryRefreshBossTitleFor(_body);
                }
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);
            }
        }
    }
}
