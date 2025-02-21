﻿using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("give_money")]
    public sealed class GiveMoney : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _amountToGive =
            ConfigFactory<int>.CreateConfig("Base Amount to Give", 200)
                              .Description("The base amount of money to give to all players")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
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

                return ClampedConversion.UInt32(amount);
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                uint amount = scaledAmountToGive;
                if (amount == 0)
                    return;

                PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
                {
                    playerMaster.GiveMoney(amount);
                }, Util.GetBestMasterName);
            }
        }
    }
}
