using HG;
using MonoMod.Utils;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Cost;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosTimedEffect("all_costs_health", TimedEffectType.UntilStageEnd, AllowDuplicates = false, DefaultSelectionWeight = 0.8f)]
    [EffectConfigBackwardsCompatibility("Effect: Blood Money (Lasts 1 stage)")]
    public sealed class AllCostsHealth : NetworkBehaviour
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static ConfigHolder<bool>[] _enabledConfigByCostType = [];

        [SystemInitializer(typeof(CostTypeCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _enabledConfigByCostType = new ConfigHolder<bool>[CostTypeCatalog.costTypeCount];

            for (CostTypeIndex i = 0; i < (CostTypeIndex)CostTypeCatalog.costTypeCount; i++)
            {
                if (i == CostTypeIndex.None || i == CostTypeIndex.PercentHealth)
                    continue;

                CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(i);
                if (costTypeDef is null)
                    continue;

                string costName = i switch
                {
                    CostTypeIndex.VolatileBattery => "Fuel Array",
                    CostTypeIndex.ArtifactShellKillerItem => "Artifact Key",
                    CostTypeIndex.TreasureCacheItem => "Rusted Key",
                    CostTypeIndex.TreasureCacheVoidItem => "Encrusted Key",
                    _ => costTypeDef.name.SpacedPascalCase()
                };

                bool defaultEnabled = i switch
                {
                    CostTypeIndex.Money or CostTypeIndex.LunarCoin or CostTypeIndex.VoidCoin or CostTypeIndex.SoulCost => true,
                    _ => false
                };

                ConfigHolder<bool> costTypeEnabledConfig =
                    ConfigFactory<bool>.CreateConfig($"Convert {costName} Costs", defaultEnabled)
                                       .Description($"If the effect should be able to turn {costName} costs into health costs")
                                       .OptionConfig(new CheckBoxConfig())
                                       .Build();

                costTypeEnabledConfig.Bind(_effectInfo);

                _enabledConfigByCostType[(int)i] = costTypeEnabledConfig;
            }
        }

        static bool canConvertCostType(CostTypeIndex costType)
        {
            ConfigHolder<bool> enabledConfig = ArrayUtils.GetSafe(_enabledConfigByCostType, (int)costType);
            return enabledConfig is not null && enabledConfig.Value;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return CostModificationManager.Instance;
        }

        ValueModificationController _costConverterController;
        CostConversionProvider _costConversionProvider;

        void Start()
        {
            if (NetworkServer.active)
            {
                _costConverterController = Instantiate(RoCContent.NetworkedPrefabs.CostConversionProvider).GetComponent<ValueModificationController>();

                _costConversionProvider = _costConverterController.GetComponent<CostConversionProvider>();
                refreshCostConversions();

                NetworkServer.Spawn(_costConverterController.gameObject);

                foreach (ConfigHolder<bool> convertCostTypeEnabledConfig in _enabledConfigByCostType)
                {
                    if (convertCostTypeEnabledConfig != null)
                    {
                        convertCostTypeEnabledConfig.SettingChanged += onCostTypeConversinEnabledChanged;
                    }
                }

                CharacterMoneyChangedHook.OnCharacterMoneyChanged += onCharacterMoneyChanged;
            }
        }

        void OnDestroy()
        {
            if (_costConverterController)
            {
                _costConverterController.Retire();
                _costConverterController = null;
                _costConversionProvider = null;
            }

            foreach (ConfigHolder<bool> convertCostTypeEnabledConfig in _enabledConfigByCostType)
            {
                if (convertCostTypeEnabledConfig != null)
                {
                    convertCostTypeEnabledConfig.SettingChanged -= onCostTypeConversinEnabledChanged;
                }
            }

            CharacterMoneyChangedHook.OnCharacterMoneyChanged -= onCharacterMoneyChanged;
        }

        void onCostTypeConversinEnabledChanged(object sender, ConfigChangedArgs<bool> e)
        {
            refreshCostConversions();
        }

        [Server]
        void refreshCostConversions()
        {
            if (!_costConversionProvider)
                return;

            for (CostTypeIndex i = 0; (int)i < CostTypeCatalog.costTypeCount; i++)
            {
                bool shouldConvert = canConvertCostType(i);

                _costConversionProvider.SetCostTypeConversion(i, shouldConvert ? CostTypeIndex.PercentHealth : null);
            }
        }

        void onCharacterMoneyChanged(CharacterMaster master, long moneyDiff)
        {
            if (moneyDiff <= 0 || !master.playerCharacterMasterController)
                return;

            CharacterBody body = master.GetBody();
            if (!body)
                return;

            float difficultyCoefficient = Stage.instance ? Stage.instance.entryDifficultyCoefficient : 1f;

            float healFraction = CostUtils.ConvertCost(moneyDiff / Mathf.Pow(difficultyCoefficient, 1.25f), CostTypeIndex.Money, CostTypeIndex.PercentHealth) / 100f;

            healFraction /= 3.5f;

            if (healFraction > 0.01f)
            {
#if DEBUG
                Log.Debug($"Healing {Util.GetBestMasterName(master)} for {healFraction:P} health (+${moneyDiff})");
#endif

                body.healthComponent.HealFraction(healFraction, new ProcChainMask());
            }
            else
            {
#if DEBUG
                Log.Debug($"Not healing {Util.GetBestMasterName(master)}, below threshold heal: {healFraction:P} (+${moneyDiff})");
#endif
            }
        }
    }
}
