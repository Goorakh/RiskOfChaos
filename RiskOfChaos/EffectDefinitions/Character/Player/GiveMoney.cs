using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("give_money", EffectWeightReductionPercentagePerActivation = 0f)]
    public sealed class GiveMoney : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _amountToGive =
            ConfigFactory<int>.CreateConfig("Base Amount to Give", 200)
                              .Description("The base amount of money to give to all players")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 0,
                                  max = 1000
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0))
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _useDifficultyScaling =
            ConfigFactory<bool>.CreateConfig("Scale Amount with Difficulty", true)
                               .Description("If the amount given should be scaled by difficulty. If this option is enabled, setting the amount to 25 will always give enough money for a regular chest for example")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        static uint scaledAmountToGive
        {
            get
            {
                int amount = _amountToGive.Value;
                if (amount > 0 && _useDifficultyScaling.Value && Run.instance)
                {
                    amount = Run.instance.GetDifficultyScaledCost(amount);
                }

                return (uint)amount;
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return TeamManager.instance;
        }

        public override void OnStart()
        {
            uint amount = scaledAmountToGive;
            if (amount == 0)
                return;

            TeamManager.instance.GiveTeamMoney(TeamIndex.Player, amount);
        }
    }
}
