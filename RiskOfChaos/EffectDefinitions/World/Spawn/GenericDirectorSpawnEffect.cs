using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    public abstract class GenericDirectorSpawnEffect<TSpawnCard> : GenericSpawnEffect<TSpawnCard> where TSpawnCard : SpawnCard
    {
        protected class SpawnCardEntry : SpawnEntry
        {
            public SpawnCardEntry(TSpawnCard[] items, float weight) : base(items, weight)
            {
            }

            public SpawnCardEntry(TSpawnCard item, float weight) : base(item, weight)
            {
            }

            protected override bool isItemAvailable(TSpawnCard spawnCard)
            {
                return base.isItemAvailable(spawnCard) && spawnCard.HasValidSpawnLocation() && isPrefabAvailable(spawnCard.prefab);
            }

            protected virtual bool isPrefabAvailable(GameObject prefab)
            {
                return prefab && ExpansionUtils.IsObjectExpansionAvailable(prefab);
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return DirectorCore.instance;
        }
    }
}
