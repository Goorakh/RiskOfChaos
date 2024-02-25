using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.CatalogIndexCollection;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosEffect("activate_random_equipment")]
    [IncompatibleEffects(typeof(DisableEquipmentActivation))]
    public sealed class ActivateRandomEquipment : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowNonPlayerEquipmentUse =
            ConfigFactory<bool>.CreateConfig("Allow Non-Player Equipment Use", true)
                               .Description("If the effect should also activate equipments on non-player characters")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        static readonly EquipmentIndexCollection _equipmentsBlacklist = new EquipmentIndexCollection([
            "EliteSecretSpeedEquipment", // Does nothing
            "GhostGun", // Requires target
            "GoldGat", // Doesn't really work in this context
            "IrradiatingLaser", // Does nothing
            "MultiShopCard", // Does nothing on activate
            "QuestVolatileBattery", // Does nothing on activate
        ]);

        readonly struct ActivatableEquipment
        {
            public readonly string EquipmentName;

            readonly EquipmentDef _equipmentDef;
            readonly ConfigHolder<float> _equipmentWeightConfig;

            public ActivatableEquipment(EquipmentDef equipmentDef)
            {
                _equipmentDef = equipmentDef;

                EquipmentName = Language.GetString(equipmentDef.nameToken, "en");

                _equipmentWeightConfig = ConfigFactory<float>.CreateConfig($"{EquipmentName} Weight", 1f)
                                                             .Description($"How likely {EquipmentName} is to be selected")
                                                             .AcceptableValues(new AcceptableValueMin<float>(0f))
                                                             .OptionConfig(new StepSliderConfig
                                                             {
                                                                 formatString = "{0:F1}",
                                                                 min = 0f,
                                                                 max = 2f,
                                                                 increment = 0.1f
                                                             })
                                                             .Build();
            }

            public readonly bool IsAvailable => _equipmentWeightConfig.Value > 0f && (!Run.instance || Run.instance.IsEquipmentEnabled(_equipmentDef.equipmentIndex));

            public readonly void BindConfig(ChaosEffectInfo effectInfo)
            {
                _equipmentWeightConfig.Bind(effectInfo);
            }

            public readonly void AddToWeightedSelection(WeightedSelection<EquipmentDef> selection)
            {
                selection.AddChoice(_equipmentDef, _equipmentWeightConfig.Value);
            }
        }

        static ActivatableEquipment[] _availableEquipments = [];

        static bool isValidEquipment(EquipmentDef equipment)
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

            if (_equipmentsBlacklist.Contains(equipment.equipmentIndex))
            {
#if DEBUG
                Log.Debug($"excluding equipment {equipment.name} ({Language.GetString(equipment.nameToken)}): blacklist");
#endif
                return false;
            }

#if DEBUG
            Log.Debug($"including equipment {equipment.name} ({Language.GetString(equipment.nameToken)})");
#endif

            return true;
        }

        [SystemInitializer(typeof(EquipmentCatalog), typeof(EliteCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _availableEquipments = EquipmentCatalog.allEquipment.Select(EquipmentCatalog.GetEquipmentDef)
                                                                .Where(isValidEquipment)
                                                                .Select(e => new ActivatableEquipment(e))
                                                                .ToArray();

            foreach (ActivatableEquipment equipment in _availableEquipments.OrderBy(a => a.EquipmentName))
            {
                equipment.BindConfig(_effectInfo);
            }
        }

        static IEnumerable<ActivatableEquipment> getAllAvailableEquipments()
        {
            return _availableEquipments.Where(e => e.IsAvailable);
        }

        static IEnumerable<CharacterBody> getAllEquipmentActivators()
        {
            IEnumerable<CharacterBody> activatorBodies;
            if (_allowNonPlayerEquipmentUse.Value)
            {
                activatorBodies = CharacterBody.readOnlyInstancesList.Where(c => c.healthComponent && c.healthComponent.alive);
            }
            else
            {
                activatorBodies = PlayerUtils.GetAllPlayerBodies(true);
            }

            return activatorBodies.Where(c => c.equipmentSlot);
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return getAllAvailableEquipments().Any() && (!context.IsNow || getAllEquipmentActivators().Any());
        }

        EquipmentDef[] _equipmentActivationOrder = [];

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            ActivatableEquipment[] availableEquipments = getAllAvailableEquipments().ToArray();
            int availableEquipmentsCount = availableEquipments.Length;

            WeightedSelection<EquipmentDef> equipmentSelector = new WeightedSelection<EquipmentDef>(availableEquipmentsCount);
            for (int i = 0; i < availableEquipmentsCount; i++)
            {
                availableEquipments[i].AddToWeightedSelection(equipmentSelector);
            }

            _equipmentActivationOrder = new EquipmentDef[equipmentSelector.Count];
            for (int i = 0; i < _equipmentActivationOrder.Length; i++)
            {
                _equipmentActivationOrder[i] = equipmentSelector.GetAndRemoveRandom(RNG);
            }
        }

        public override void OnStart()
        {
            // ToArray since equipments might modify the underlying collection by spawning a new character
            getAllEquipmentActivators().ToArray().TryDo(activateRandomEquipment, FormatUtils.GetBestBodyName);
        }

        void activateRandomEquipment(CharacterBody body)
        {
            EquipmentSlot equipmentSlot = body.equipmentSlot;

            foreach (EquipmentDef equipment in _equipmentActivationOrder)
            {
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
                    Log.Warning_NoCallerPrefix($"Caught exception when trying to activate equipment \"{Language.GetString(equipment.nameToken)}\" on {FormatUtils.GetBestBodyName(body)}: {ex}");
                }

                if (equipmentSuccessfullyPerformed)
                {
#if DEBUG
                    Log.Debug($"Activated equipment \"{Language.GetString(equipment.nameToken)}\" on {FormatUtils.GetBestBodyName(body)}");
#endif
                    return;
                }
#if DEBUG
                else
                {
                    Log.Debug($"{Language.GetString(equipment.nameToken)} was not activatable for {FormatUtils.GetBestBodyName(body)}");
                }
#endif
            }

            Log.Warning($"no equipment was activatable for {FormatUtils.GetBestBodyName(body)}");
        }
    }
}
