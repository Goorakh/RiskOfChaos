using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.ObjectModel;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("duplicate_all_characters", EffectWeightReductionPercentagePerActivation = 50f)]
    public sealed class DuplicateAllCharacters : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDontDestroyOnLoad =
            ConfigFactory<bool>.CreateConfig("Keep duplicated allies between stages", false)
                               .Description("Allows duplicated allies to come with you to the next stage.\nThis is disabled by default to prevent lag by repeatedly duplicating your drones.")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        public override void OnStart()
        {
            ReadOnlyCollection<CharacterBody> allCharacterBodies = CharacterBody.readOnlyInstancesList;
            for (int i = allCharacterBodies.Count - 1; i >= 0; i--)
            {
                CharacterBody body = allCharacterBodies[i];
                if (!body)
                    continue;

                if ((body.bodyFlags & CharacterBody.BodyFlags.Masterless) != 0)
                    continue;

                CharacterMaster master = body.master;
                if (!master)
                    continue;

                try
                {
                    duplicateMaster(master);
                }
                catch (Exception ex)
                {
                    Log.Error_NoCallerPrefix($"Failed to duplicate body {FormatUtils.GetBestBodyName(body)}: {ex}");
                }
            }
        }

        void duplicateMaster(CharacterMaster master)
        {
            CharacterBody body = master.GetBody();
            if (!body)
                return;

            MasterCopySpawnCard copySpawnCard = MasterCopySpawnCard.FromMaster(master, true, true);

            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode = SpawnUtils.ExtraPlacementModes.NearestNodeWithConditions,
                position = body.footPosition,
                minDistance = body.bestFitRadius * 2f,
                maxDistance = float.PositiveInfinity
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(copySpawnCard, placementRule, RNG.Branch())
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
                    BossGroup bossGroup = BossGroup.FindBossGroup(body);
                    if (bossGroup && bossGroup.combatSquad)
                    {
                        bossGroup.combatSquad.AddMember(spawnedMaster);
                    }
                }
            };

            spawnRequest.SpawnWithFallbackPlacement(new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = body.footPosition
            });

            UnityEngine.Object.Destroy(copySpawnCard);
        }
    }
}
