using RiskOfChaos.Content;
using RiskOfChaos.Content.Logbook;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Stats;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_invincible_lemurian")]
    public sealed class SpawnIncincibleLemurian : GenericDirectorSpawnEffect<CharacterSpawnCard>
    {
        class LemurianSpawnEntry : SpawnCardEntry
        {
            public readonly bool IsElder;

            public LemurianSpawnEntry(string addressablePath, float weight, bool isElder) : base(addressablePath, weight)
            {
                IsElder = isElder;
            }
        }

        static LemurianSpawnEntry[] _entries = [];

        static LemurianSpawnEntry loadBasicSpawnEntry(string addressablePath, float weight, bool isElder)
        {
            return new LemurianSpawnEntry(addressablePath, weight, isElder);
        }

        [SystemInitializer]
        static void Init()
        {
            _entries = [
                loadBasicSpawnEntry("RoR2/Base/Lemurian/cscLemurian.asset", 95f, false),
                loadBasicSpawnEntry("RoR2/Base/LemurianBruiser/cscLemurianBruiser.asset", 5f, true)
            ];
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_entries);
        }

        public override void OnStart()
        {
            CharacterSpawnCard spawnCard = getItemToSpawn(_entries, RNG, out LemurianSpawnEntry spawnEntry);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, SpawnUtils.GetBestValidRandomPlacementRule(), RNG)
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

#if DEBUG
            InvincibleLemurianLogbookAdder.LemurianStatCollection lemurianStatCollection = InvincibleLemurianLogbookAdder.GetStatCollection(spawnEntry.IsElder);
#endif

            spawnRequest.onSpawnedServer = result =>
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

#if DEBUG
                foreach (PlayerStatsComponent statsComponent in PlayerStatsComponent.instancesList)
                {
                    statsComponent.currentStats.PushStatValue(lemurianStatCollection.EncounteredStat, 1);
                }
#endif
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
