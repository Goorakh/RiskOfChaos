using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosEffect("activate_random_equipment")]
    [IncompatibleEffects(typeof(DisableEquipmentActivation))]
    public sealed class ActivateRandomEquipment : NetworkBehaviour
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowNonPlayerEquipmentUse =
            ConfigFactory<bool>.CreateConfig("Allow Non-Player Equipment Use", false)
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
                                                             .Description($"How likely {EquipmentName} is to be selected, set to 0 to exclude from the effect")
                                                             .AcceptableValues(new AcceptableValueMin<float>(0f))
                                                             .OptionConfig(new FloatFieldConfig { Min = 0f })
                                                             .Build();
            }

            public readonly bool IsAvailable
            {
                get
                {
                    if (!_equipmentDef || _equipmentWeightConfig.Value <= 0f)
                        return false;

                    if (_equipmentDef.canDrop)
                    {
                        if (Run.instance && !Run.instance.IsEquipmentEnabled(_equipmentDef.equipmentIndex))
                            return false;
                    }

                    return true;
                }
            }

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
            if (string.IsNullOrWhiteSpace(equipment.nameToken) || Language.IsTokenInvalid(equipment.nameToken))
            {
                Log.Debug($"excluding equipment {equipment.name} ({equipment.nameToken}): Invalid name token");
                return false;
            }

            if (EliteCatalog.eliteList.Select(EliteCatalog.GetEliteDef)
                                      .Any(elite => elite.eliteEquipmentDef == equipment))
            {
                Log.Debug($"excluding equipment {equipment.name} ({Language.GetString(equipment.nameToken)}): elite affix");
                return false;
            }

            bool isNullModel;
            if (equipment.pickupModelReference != null && equipment.pickupModelReference.RuntimeKeyIsValid())
            {
                isNullModel = equipment.pickupModelReference.AssetGUID == AddressableGuids.RoR2_Base_Core_NullModel_prefab;
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                isNullModel = !equipment.pickupModelPrefab || equipment.pickupModelPrefab.name == "NullModel";
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (isNullModel)
            {
                Log.Debug($"excluding equipment {equipment.name} ({Language.GetString(equipment.nameToken)}): null model");
                return false;
            }

            if (_equipmentsBlacklist.Contains(equipment.equipmentIndex))
            {
                Log.Debug($"excluding equipment {equipment.name} ({Language.GetString(equipment.nameToken)}): blacklist");
                return false;
            }

            Log.Debug($"including equipment {equipment.name} ({Language.GetString(equipment.nameToken)})");

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

        ChaosEffectComponent _effectComponent;

        EquipmentDef[] _equipmentActivationOrder = [];

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            ActivatableEquipment[] availableEquipments = getAllAvailableEquipments().ToArray();
            int availableEquipmentsCount = availableEquipments.Length;

            WeightedSelection<EquipmentDef> equipmentSelector = new WeightedSelection<EquipmentDef>();
            equipmentSelector.EnsureCapacity(availableEquipmentsCount);
            for (int i = 0; i < availableEquipmentsCount; i++)
            {
                availableEquipments[i].AddToWeightedSelection(equipmentSelector);
            }

            Xoroshiro128Plus equipmentOrderRng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _equipmentActivationOrder = new EquipmentDef[equipmentSelector.Count];
            for (int i = 0; i < _equipmentActivationOrder.Length; i++)
            {
                _equipmentActivationOrder[i] = equipmentSelector.GetAndRemoveRandom(equipmentOrderRng);
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                // ToArray since equipments might modify the underlying collection by spawning a new character
                getAllEquipmentActivators().ToArray().TryDo(activateRandomEquipment, FormatUtils.GetBestBodyName);
            }
        }

        [Server]
        void activateRandomEquipment(CharacterBody body)
        {
            EquipmentSlot equipmentSlot = body.equipmentSlot;

            foreach (EquipmentDef equipment in _equipmentActivationOrder)
            {
                bool equipmentSuccessfullyPerformed;
                try
                {
                    equipmentSuccessfullyPerformed = equipmentSlot.PerformEquipmentAction(equipment);
                }
                catch (Exception ex)
                {
                    equipmentSuccessfullyPerformed = false;
                    Log.Warning_NoCallerPrefix($"Caught exception when trying to activate equipment \"{Language.GetString(equipment.nameToken)}\" on {FormatUtils.GetBestBodyName(body)}: {ex}");
                }

                if (equipmentSuccessfullyPerformed)
                {
                    Log.Debug($"Activated equipment \"{Language.GetString(equipment.nameToken)}\" on {FormatUtils.GetBestBodyName(body)}");
                    return;
                }
                else
                {
                    Log.Debug($"{Language.GetString(equipment.nameToken)} was not activatable for {FormatUtils.GetBestBodyName(body)}");
                }
            }

            Log.Warning($"no equipment was activatable for {FormatUtils.GetBestBodyName(body)}");
        }
    }
}
