using RoR2;
using RoR2.ExpansionManagement;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities
{
    public static class SpawnPoolUtils
    {
        public static readonly SpawnPool<CharacterSpawnCard>.RequiredExpansionsProviderDelegate CharacterSpawnCardExpansionsProvider = CharacterSpawnCardExpansionsProviderImpl;

        public static readonly SpawnPool<InteractableSpawnCard>.RequiredExpansionsProviderDelegate InteractableSpawnCardExpansionsProvider = InteractableSpawnCardExpansionsProviderImpl;

        public static readonly SpawnPool<BuffDef>.RequiredExpansionsProviderDelegate BuffExpansionsProvider = BuffExpansionsProviderImpl;

        public static readonly SpawnPool<EliteDef>.RequiredExpansionsProviderDelegate EliteExpansionsProvider = EliteExpansionsProviderImpl;

        public static readonly SpawnPool<EquipmentDef>.RequiredExpansionsProviderDelegate EquipmentExpansionsProvider = EquipmentExpansionsProviderImpl;

        public static IReadOnlyList<ExpansionDef> CharacterSpawnCardExpansionsProviderImpl(CharacterSpawnCard characterSpawnCard)
        {
            return SpawnCardExpansionsProviderImpl(characterSpawnCard);
        }

        public static IReadOnlyList<ExpansionDef> InteractableSpawnCardExpansionsProviderImpl(InteractableSpawnCard interactableSpawnCard)
        {
            return SpawnCardExpansionsProviderImpl(interactableSpawnCard);
        }

        public static IReadOnlyList<ExpansionDef> SpawnCardExpansionsProviderImpl(SpawnCard spawnCard)
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

        public static IReadOnlyList<ExpansionDef> BuffExpansionsProviderImpl(BuffDef buffDef)
        {
            if (!buffDef)
                return [];

            return EliteExpansionsProviderImpl(buffDef.eliteDef);
        }

        public static IReadOnlyList<ExpansionDef> EliteExpansionsProviderImpl(EliteDef eliteDef)
        {
            if (!eliteDef)
                return [];

            return EquipmentExpansionsProviderImpl(eliteDef.eliteEquipmentDef);
        }

        public static IReadOnlyList<ExpansionDef> EquipmentExpansionsProviderImpl(EquipmentDef equipmentDef)
        {
            if (equipmentDef && equipmentDef.requiredExpansion)
            {
                return [equipmentDef.requiredExpansion];
            }

            return [];
        }
    }
}
