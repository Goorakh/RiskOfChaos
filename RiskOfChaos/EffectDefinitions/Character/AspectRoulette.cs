using HarmonyLib;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("aspect_roulette")]
    [ChaosTimedEffect(90f, AllowDuplicates = false)]
    public sealed class AspectRoulette : TimedEffect
    {
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
                        inventory.SetEquipmentIndex(EliteUtils.GetRandomEliteEquipmentIndex());
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
