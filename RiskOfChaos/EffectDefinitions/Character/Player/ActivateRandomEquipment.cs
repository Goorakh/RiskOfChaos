using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("activate_random_equipment")]
    public sealed class ActivateRandomEquipment : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        readonly struct ActivatableEquipment
        {
            readonly EquipmentDef _equipmentDef;
            readonly ConfigEntry<float> _equipmentWeightConfig;

            readonly float selectionWeight
            {
                get
                {
                    if (_equipmentWeightConfig != null)
                    {
                        return Mathf.Max(_equipmentWeightConfig.Value, 0f);
                    }

                    return 1f;
                }
            }

            public ActivatableEquipment(EquipmentDef equipmentDef)
            {
                _equipmentDef = equipmentDef;

                string equipmentName = Language.GetString(equipmentDef.nameToken, "en");

                _equipmentWeightConfig = _effectInfo.BindConfig($"{equipmentName.FilterConfigKey()} Weight", 1f, new ConfigDescription($"How likely {equipmentName} is to be selected"));

                addConfigOption(new StepSliderOption(_equipmentWeightConfig, new StepSliderConfig
                {
                    formatString = "{0:F1}",
                    min = 0f,
                    max = 2f,
                    increment = 0.1f
                }));
            }

            public readonly void AddToWeightedSelection(WeightedSelection<EquipmentDef> selection)
            {
                selection.AddChoice(_equipmentDef, selectionWeight);
            }
        }

        static ActivatableEquipment[] _availableEquipments;
        static int _availableEquipmentsCount;

        [SystemInitializer(typeof(EquipmentCatalog), typeof(EliteCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _availableEquipments = Array.ConvertAll(getAllActivatableEquipmentDefs(), static ed => new ActivatableEquipment(ed));
            _availableEquipmentsCount = _availableEquipments.Length;
        }

        static EquipmentDef[] getAllActivatableEquipmentDefs()
        {
            return EquipmentCatalog.allEquipment.Select(EquipmentCatalog.GetEquipmentDef)
                                                .Where(equipment =>
                                                {
                                                    if (string.IsNullOrWhiteSpace(equipment.nameToken) || Language.GetString(equipment.nameToken) == equipment.nameToken)
                                                    {
#if DEBUG
                                                        Log.Debug($"excluding equipment {equipment.name} ({equipment.nameToken}): Invalid name token");
#endif
                                                        return false;
                                                    }

                                                    if (EliteCatalog.eliteList.Select(EliteCatalog.GetEliteDef)
                                                                              .Any(elite => elite.eliteEquipmentDef == equipment))
                                                    {
#if DEBUG
                                                        Log.Debug($"excluding equipment {equipment.name} ({Language.GetString(equipment.nameToken)}): elite affix");
#endif
                                                        return false;
                                                    }

                                                    if (!equipment.pickupModelPrefab || equipment.pickupModelPrefab.name == "NullModel")
                                                    {
#if DEBUG
                                                        Log.Debug($"excluding equipment {equipment.name} ({Language.GetString(equipment.nameToken)}): null model");
#endif
                                                        return false;
                                                    }

                                                    switch (equipment.name)
                                                    {
                                                        case "EliteSecretSpeedEquipment": // Does nothing
                                                        case "GhostGun": // Requires target
                                                        case "GoldGat": // Doesn't really work in this context
                                                        case "IrradiatingLaser": // Does nothing
                                                        case "MultiShopCard": // Does nothing on activate
                                                        case "QuestVolatileBattery": // Does nothing on activate
#if DEBUG
                                                            Log.Debug($"excluding equipment {equipment.name} ({Language.GetString(equipment.nameToken)}): blacklist");
#endif
                                                            return false;
                                                    }

#if DEBUG
                                                    Log.Debug($"including equipment {equipment.name} ({Language.GetString(equipment.nameToken)})");
#endif

                                                    return true;
                                                })
                                                .ToArray();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _availableEquipments != null && _availableEquipmentsCount > 0;
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerBodies(true).TryDo(activateRandomEquipment, FormatUtils.GetBestBodyName);
        }

        void activateRandomEquipment(CharacterBody body)
        {
            EquipmentSlot equipmentSlot = body.equipmentSlot;
            if (!equipmentSlot)
                return;

            Xoroshiro128Plus rng = new Xoroshiro128Plus(RNG.nextUlong);

            WeightedSelection<EquipmentDef> equipmentSelector = new WeightedSelection<EquipmentDef>(_availableEquipmentsCount);
            for (int i = 0; i < _availableEquipmentsCount; i++)
            {
                _availableEquipments[i].AddToWeightedSelection(equipmentSelector);
            }

            while (equipmentSelector.Count > 0)
            {
                EquipmentDef equipment = equipmentSelector.GetAndRemoveRandom(rng);

                if (!Run.instance.IsEquipmentAvailable(equipment.equipmentIndex))
                {
#if DEBUG
                    Log.Debug($"{Language.GetString(equipment.nameToken)} is not available in the current run");
#endif
                    continue;
                }

                if (Run.instance.IsEquipmentExpansionLocked(equipment.equipmentIndex))
                {
#if DEBUG
                    Log.Debug($"{Language.GetString(equipment.nameToken)} is expansion locked");
#endif
                    continue;
                }

                bool equipmentSuccessfullyPerformed;
                try
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    equipmentSuccessfullyPerformed = equipmentSlot.PerformEquipmentAction(equipment);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }
                catch (Exception ex)
                {
                    equipmentSuccessfullyPerformed = false;
                    Log.Warning_NoCallerPrefix($"Caught exception when trying to activate equipment \"{Language.GetString(equipment.nameToken)}\": {ex}");
                }

                if (equipmentSuccessfullyPerformed)
                {
#if DEBUG
                    Log.Debug($"Activated equipment \"{Language.GetString(equipment.nameToken)}\"");
#endif
                    return;
                }
#if DEBUG
                else
                {
                    Log.Debug($"{Language.GetString(equipment.nameToken)} was not activatable for {body}");
                }
#endif
            }

            Log.Warning($"no equipment was activatable for {body}");
        }
    }
}
