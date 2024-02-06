using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.CharacterAI;
using System;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_invincible_lemurian")]
    public sealed class SpawnIncincibleLemurian : GenericDirectorSpawnEffect<CharacterSpawnCard>
    {
        static SpawnCardEntry[] _entries = [];

        [SystemInitializer]
        static void Init()
        {
            _entries = [
                loadBasicSpawnEntry("RoR2/Base/Lemurian/cscLemurian.asset", 95f),
                loadBasicSpawnEntry("RoR2/Base/LemurianBruiser/cscLemurianBruiser.asset", 5f)
            ];
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_entries);
        }

        public override void OnStart()
        {
            CharacterSpawnCard spawnCard = getItemToSpawn(_entries, RNG);
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, SpawnUtils.GetBestValidRandomPlacementRule(), RNG)
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

            spawnRequest.onSpawnedServer = static result =>
            {
                if (!result.success || !result.spawnedInstance)
                    return;

                CharacterMaster characterMaster = result.spawnedInstance.GetComponent<CharacterMaster>();
                if (!characterMaster)
                    return;

                characterMaster.inventory.GiveItem(Items.InvincibleLemurianMarker);

                foreach (BaseAI baseAI in characterMaster.GetComponents<BaseAI>())
                {
                    if (baseAI)
                    {
                        baseAI.fullVision = true;
                        baseAI.neverRetaliateFriendlies = true;
                    }
                }
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
