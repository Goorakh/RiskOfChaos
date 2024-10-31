using RoR2;
using RoR2.ExpansionManagement;

namespace RiskOfChaos.Utilities
{
    public static class SpawnPoolUtils
    {
        public static readonly SpawnPool<CharacterSpawnCard>.RequiredExpansionsProviderDelegate CharacterSpawnCardExpansionsProvider = CharacterSpawnCardExpansionsProviderImpl;

        public static readonly SpawnPool<InteractableSpawnCard>.RequiredExpansionsProviderDelegate InteractableSpawnCardExpansionsProvider = InteractableSpawnCardExpansionsProviderImpl;

        public static ExpansionDef[] CharacterSpawnCardExpansionsProviderImpl(CharacterSpawnCard characterSpawnCard)
        {
            return SpawnCardExpansionsProviderImpl(characterSpawnCard);
        }

        public static ExpansionDef[] InteractableSpawnCardExpansionsProviderImpl(InteractableSpawnCard interactableSpawnCard)
        {
            return SpawnCardExpansionsProviderImpl(interactableSpawnCard);
        }

        public static ExpansionDef[] SpawnCardExpansionsProviderImpl(SpawnCard spawnCard)
        {
            if (spawnCard && spawnCard.prefab)
            {
                return ExpansionUtils.GetObjectRequiredExpansions(spawnCard.prefab);
            }
            else
            {
                return [];
            }
        }
    }
}
