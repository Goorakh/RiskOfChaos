using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    public abstract class GenericDirectorSpawnEffect<TSpawnCard> : GenericSpawnEffect<TSpawnCard> where TSpawnCard : SpawnCard
    {
        protected class SpawnCardEntry : SpawnEntry
        {
            public SpawnCardEntry(TSpawnCard[] items, float weight) : base(items, weight)
            {
            }
            public SpawnCardEntry(string[] addressablePaths, float weight) : this(Array.ConvertAll(addressablePaths, p => Addressables.LoadAssetAsync<TSpawnCard>(p).WaitForCompletion()), weight)
            {
            }

            public SpawnCardEntry(TSpawnCard item, float weight) : base(item, weight)
            {
            }

            public SpawnCardEntry(string addressablePath, float weight) : this(Addressables.LoadAssetAsync<TSpawnCard>(addressablePath).WaitForCompletion(), weight)
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

        protected static new SpawnCardEntry loadBasicSpawnEntry(string addressablePath, float weight = 1f)
        {
            return new SpawnCardEntry(addressablePath, weight);
        }

        protected static new SpawnCardEntry loadBasicSpawnEntry(string[] addressablePaths, float weight = 1f)
        {
            return new SpawnCardEntry(addressablePaths, weight);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return DirectorCore.instance;
        }
    }
}
