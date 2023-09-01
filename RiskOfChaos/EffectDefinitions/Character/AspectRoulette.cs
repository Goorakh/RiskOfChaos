using HarmonyLib;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;

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

        [RequireComponent(typeof(CharacterBody))]
        class RandomlySwapAspect : MonoBehaviour
        {
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
                    tryReplaceAspect();
                    _aspectReplaceTimer = RoR2Application.rng.RangeFloat(1f, 7.5f);
                }
            }

            void tryReplaceAspect()
            {
                if (!_body)
                    return;

                Inventory inventory = _body.inventory;
                if (!inventory)
                    return;

                EquipmentIndex currentEquipment = inventory.GetEquipmentIndex();
                if (!_body.isPlayerControlled || currentEquipment == EquipmentIndex.None || EliteUtils.IsEliteEquipment(currentEquipment))
                {
                    if (EliteUtils.HasAnyAvailableEliteEquipments)
                    {
                        inventory.SetEquipmentIndex(EliteUtils.SelectEliteEquipment(_allowDirectorUnavailableElites.Value));
                    }
                    else
                    {
                        Log.Error($"{nameof(EliteUtils)} is not initialized");
                    }
                }
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);
            }
        }

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.Do(tryAddComponentToBody);

            CharacterBody.onBodyStartGlobal += tryAddComponentToBody;
        }

        static void tryAddComponentToBody(CharacterBody body)
        {
            try
            {
                body.gameObject.AddComponent<RandomlySwapAspect>();
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
    }
}
