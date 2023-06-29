using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("give_money", EffectWeightReductionPercentagePerActivation = 0f)]
    public sealed class GiveMoney : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        const int AMOUNT_TO_GIVE_DEFAULT_VALUE = 200;
        static ConfigEntry<int> _amountToGive;
        static int amountToGive
        {
            get
            {
                if (_amountToGive == null)
                {
                    return AMOUNT_TO_GIVE_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_amountToGive.Value, 0);
                }
            }
        }

        const bool USE_DIFFICULTY_SCALING_DEFAULT_VALUE = true;
        static ConfigEntry<bool> _useDifficultyScaling;
        static bool useDifficultyScaling => _useDifficultyScaling?.Value ?? USE_DIFFICULTY_SCALING_DEFAULT_VALUE;

        static uint scaledAmountToGive
        {
            get
            {
                int amount = amountToGive;
                if (amount > 0 && useDifficultyScaling && Run.instance)
                {
                    amount = Run.instance.GetDifficultyScaledCost(amount);
                }

                return (uint)amount;
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _amountToGive = _effectInfo.BindConfig("Base Amount to Give", AMOUNT_TO_GIVE_DEFAULT_VALUE, new ConfigDescription("The base amount of money to give to all players"));
            addConfigOption(new IntSliderOption(_amountToGive, new IntSliderConfig
            {
                min = 0,
                max = 1000
            }));

            _useDifficultyScaling = _effectInfo.BindConfig("Scale Amount with Difficulty", USE_DIFFICULTY_SCALING_DEFAULT_VALUE, new ConfigDescription("If the amount given should be scaled by difficulty. If this option is enabled, setting the amount to 25 will always give enough money for a regular chest for example"));
            addConfigOption(new CheckBoxOption(_useDifficultyScaling));
        }

        public override void OnStart()
        {
            uint amount = scaledAmountToGive;
            if (amount == 0)
                return;

            PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
            {
                master.GiveMoney(amount);
            }, Util.GetBestMasterName);
        }
    }
}
