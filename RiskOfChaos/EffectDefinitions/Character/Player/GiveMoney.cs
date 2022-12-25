using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utility;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect(EFFECT_ID, EffectRepetitionWeightExponent = 0f)]
    public class GiveMoney : BaseEffect
    {
        const string EFFECT_ID = "GiveMoney";

        const int AMOUNT_TO_GIVE_DEFAULT_VALUE = 200;
        static ConfigEntry<int> _amountToGive;
        static int amountToGive => _amountToGive?.Value ?? AMOUNT_TO_GIVE_DEFAULT_VALUE;

        const bool USE_DIFFICULTY_SCALING_DEFAULT_VALUE = true;
        static ConfigEntry<bool> _useDifficultyScaling;
        static bool useDifficultyScaling => _useDifficultyScaling?.Value ?? USE_DIFFICULTY_SCALING_DEFAULT_VALUE;

        static uint scaledAmountToGive
        {
            get
            {
                int amount = amountToGive;
                if (useDifficultyScaling && Run.instance)
                {
                    amount = Run.instance.GetDifficultyScaledCost(amount);
                }

                return (uint)amount;
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            string configSectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            _amountToGive = Main.Instance.Config.Bind(new ConfigDefinition(configSectionName, "Base Amount to Give"), AMOUNT_TO_GIVE_DEFAULT_VALUE, new ConfigDescription("The base amount of money to give to all players"));
            ChaosEffectCatalog.AddEffectConfigOption(new IntSliderOption(_amountToGive, new IntSliderConfig
            {
                min = 0,
                max = 1000
            }));

            _useDifficultyScaling = Main.Instance.Config.Bind(new ConfigDefinition(configSectionName, "Scale Amount with Difficulty"), USE_DIFFICULTY_SCALING_DEFAULT_VALUE, new ConfigDescription("If the amount given should be scaled by difficulty. If this option is enabled, setting the amount to 25 will always give enough money for a regular chest for example"));
            ChaosEffectCatalog.AddEffectConfigOption(new CheckBoxOption(_useDifficultyScaling));
        }

        public override void OnStart()
        {
            uint amount = scaledAmountToGive;

            foreach (CharacterMaster master in PlayerUtils.GetAllPlayerMasters(false))
            {
                master.GiveMoney(amount);
            }
        }
    }
}
