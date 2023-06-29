using HarmonyLib;
using R2API.Utils;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("aspect_roulette")]
    [ChaosTimedEffect(90f, AllowDuplicates = false)]
    public sealed class AspectRoulette : TimedEffect
    {
        static EquipmentIndex[] _aspectEquipmentIndices = Array.Empty<EquipmentIndex>();

        [SystemInitializer(typeof(EliteCatalog))]
        static void InitEquipments()
        {
            _aspectEquipmentIndices = EliteCatalog.eliteList
                                                  .Select(i => EliteCatalog.GetEliteDef(i).eliteEquipmentDef)
                                                  .Where(ed => ed.pickupModelPrefab && ed.pickupModelPrefab.name != "NullModel" && ed.dropOnDeathChance > 0f)
                                                  .Select(ed => ed.equipmentIndex)
                                                  .OrderBy(i => i)
                                                  .ToArray();
        }

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
                if (!_body.isPlayerControlled || currentEquipment == EquipmentIndex.None || Array.BinarySearch(_aspectEquipmentIndices, currentEquipment) >= 0)
                {
                    if (_aspectEquipmentIndices != null && _aspectEquipmentIndices.Length > 0)
                    {
                        inventory.SetEquipmentIndex(RoR2Application.rng.NextElementUniform(_aspectEquipmentIndices));
                    }
                    else
                    {
                        Log.Error($"{nameof(_aspectEquipmentIndices)} is not initialized");
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
