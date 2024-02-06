using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("void_implosion_on_death", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility([
        "Effect: Void Implosion on Death"
    ])]
    public sealed class VoidImplosionOnDeath : TimedEffect
    {
        readonly record struct SerializableProjectilePrefab(string AssetPath)
        {
            public readonly GameObject GetPrefab()
            {
                return Addressables.LoadAssetAsync<GameObject>(AssetPath).WaitForCompletion();
            }
        }

        static readonly WeightedSelection<SerializableProjectilePrefab> _prefabSelection;

        static VoidImplosionOnDeath()
        {
            _prefabSelection = new WeightedSelection<SerializableProjectilePrefab>();
            _prefabSelection.AddChoice(new SerializableProjectilePrefab("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab"), 1f);
            _prefabSelection.AddChoice(new SerializableProjectilePrefab("RoR2/DLC1/VoidJailer/VoidJailerDeathBombProjectile.prefab"), 0.3f);
            _prefabSelection.AddChoice(new SerializableProjectilePrefab("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombProjectile.prefab"), 0.7f);
        }

        readonly SerializableProjectilePrefab[] _projectilePrefabByBodyIndex = new SerializableProjectilePrefab[BodyCatalog.bodyCount];

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            for (int i = 0; i < _projectilePrefabByBodyIndex.Length; i++)
            {
                _projectilePrefabByBodyIndex[i] = _prefabSelection.Evaluate(RNG.nextNormalizedFloat);
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            Dictionary<string, uint> prefabPathToIndex = [];

            string[] prefabPaths = _projectilePrefabByBodyIndex.Select(p => p.AssetPath).Distinct().ToArray();

            writer.WritePackedUInt32((uint)prefabPaths.Length);
            for (uint i = 0; i < prefabPaths.Length; i++)
            {
                writer.Write(prefabPaths[i]);
                prefabPathToIndex.Add(prefabPaths[i], i);
            }

            writer.WritePackedUInt32((uint)_projectilePrefabByBodyIndex.Length);
            foreach (SerializableProjectilePrefab prefab in _projectilePrefabByBodyIndex)
            {
                writer.WritePackedUInt32(prefabPathToIndex[prefab.AssetPath]);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            Dictionary<uint, string> indexToPrefabPath = [];

            uint prefabPathsLength = reader.ReadPackedUInt32();
            for (uint i = 0; i < prefabPathsLength; i++)
            {
                indexToPrefabPath.Add(i, reader.ReadString());
            }

            uint serializedProjectilesCount = reader.ReadPackedUInt32();
            if (serializedProjectilesCount != _projectilePrefabByBodyIndex.Length)
            {
                Log.Warning($"Unmatching body counts! Expected: {_projectilePrefabByBodyIndex.Length}, Read: {serializedProjectilesCount}");
            }

            long projectileCountToRead = Math.Min(serializedProjectilesCount, _projectilePrefabByBodyIndex.Length);
            for (int i = 0; i < projectileCountToRead; i++)
            {
                _projectilePrefabByBodyIndex[i] = new SerializableProjectilePrefab(indexToPrefabPath[reader.ReadPackedUInt32()]);
            }

            if (serializedProjectilesCount < _projectilePrefabByBodyIndex.Length)
            {
                Log.Warning($"Missing {_projectilePrefabByBodyIndex.Length - serializedProjectilesCount} entries, generating fallbacks");

                for (uint i = serializedProjectilesCount; i < _projectilePrefabByBodyIndex.Length; i++)
                {
                    _projectilePrefabByBodyIndex[i] = _prefabSelection.GetChoice(0).value;
                }
            }
            else if (serializedProjectilesCount > _projectilePrefabByBodyIndex.Length)
            {
                Log.Warning($"{serializedProjectilesCount - _projectilePrefabByBodyIndex.Length} unexpected entries in serialized data, ignoring");

                for (long i = serializedProjectilesCount - _projectilePrefabByBodyIndex.Length - 1; i >= 0; i--)
                {
                    reader.ReadPackedUInt32();
                }
            }
        }

        public override void OnStart()
        {
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        }

        public override void OnEnd()
        {
            GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
        }

        void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            if (!NetworkServer.active || !ProjectileManager.instance)
                return;

            if (report.victimBodyIndex == BodyIndex.None)
                return;

            int index = (int)report.victimBodyIndex;
            if (!ArrayUtils.IsInBounds(_projectilePrefabByBodyIndex, index))
                return;

            SerializableProjectilePrefab projectile = _projectilePrefabByBodyIndex[index];

            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = projectile.GetPrefab(),
                position = report.victimBody.corePosition,
                rotation = Quaternion.identity
            });
        }
    }
}
