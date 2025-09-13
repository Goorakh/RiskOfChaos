using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectUtils.World.Spawn;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("duplicate_all_characters", DefaultSelectionWeight = 0.5f)]
    public sealed class DuplicateAllCharacters : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDontDestroyOnLoad =
            ConfigFactory<bool>.CreateConfig("Keep duplicated allies between stages", false)
                               .Description("""
                                Allows duplicated allies to come with you to the next stage.
                                This is disabled by default to prevent lag by repeatedly duplicating your drones.
                                """)
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        void Start()
        {
            if (!NetworkServer.active)
                return;
            
            List<CharacterBody> charactersToDuplicate = new List<CharacterBody>(CharacterMaster.readOnlyInstancesList.Count);
            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                if (master)
                {
                    CharacterBody body = master.GetBody();
                    if (body)
                    {
                        charactersToDuplicate.Add(body);
                    }
                }
            }

            foreach (CharacterBody body in charactersToDuplicate)
            {
                try
                {
                    duplicateCharacter(body);
                }
                catch (Exception ex)
                {
                    Log.Error_NoCallerPrefix($"Failed to duplicate body {FormatUtils.GetBestBodyName(body)}: {ex}");
                }
            }
        }

        static void duplicateCharacter(CharacterBody body)
        {
            if (!body)
                return;

            CharacterMaster master = body.master;
            if (!master)
                return;
            
            MasterCopySpawnCard copySpawnCard = MasterCopySpawnCard.FromMaster(master, true, true);
            copySpawnCard.nodeGraphType = CombatCharacterSpawnHelper.GetSpawnGraphType(master);
            copySpawnCard.forbiddenFlags |= NodeFlags.NoCharacterSpawn;

            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode = SpawnUtils.ExtraPlacementModes.NearestNodeWithConditions,
                position = body.footPosition,
                minDistance = body.bestFitActualRadius * 2f,
                maxDistance = float.PositiveInfinity
            };
            
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(copySpawnCard, placementRule, RoR2Application.rng)
            {
                summonerBodyObject = body.gameObject,
                teamIndexOverride = master.teamIndex,
                ignoreTeamMemberLimit = true
            };

            spawnRequest.onSpawnedServer += result =>
            {
                if (!result.success || !result.spawnedInstance)
                    return;

                result.spawnedInstance.SetDontDestroyOnLoad(_allowDontDestroyOnLoad.Value && Util.IsDontDestroyOnLoad(master.gameObject));

                if (result.spawnedInstance.TryGetComponent(out CharacterMaster spawnedMaster))
                {
                    foreach (CombatSquad combatSquad in InstanceTracker.GetInstancesList<CombatSquad>())
                    {
                        if (!combatSquad.propagateMembershipToSummons && combatSquad.HasContainedMember(master.netId))
                        {
                            combatSquad.AddMember(spawnedMaster);
                        }
                    }
                }
            };

            spawnRequest.SpawnWithFallbackPlacement(new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = body.footPosition
            });

            Destroy(copySpawnCard);
        }
    }
}
