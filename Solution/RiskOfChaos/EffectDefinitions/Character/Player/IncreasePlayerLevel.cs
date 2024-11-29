using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("increase_player_level", ConfigName = "Increase Player Level")]
    public sealed class IncreasePlayerLevel : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _levelsToAdd =
            ConfigFactory<int>.CreateConfig("Levels to Add", 5)
                              .Description("The amount of levels to add to all players")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericInt32(_levelsToAdd);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return TeamManager.instance && TeamManager.instance.GetTeamLevel(TeamIndex.Player) < TeamManager.naturalLevelCap;
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                TeamManager teamManager = TeamManager.instance;
                if (teamManager)
                {
                    uint newLevel = teamManager.GetTeamLevel(TeamIndex.Player) + ClampedConversion.UInt32(_levelsToAdd.Value);
                    teamManager.SetTeamLevel(TeamIndex.Player, newLevel);
                }
            }
        }
    }
}
