using HG;
using Newtonsoft.Json;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Serialization.Converters;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("void_implosion_on_death", 60f, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: Void Implosion on Death")]
    public sealed class VoidImplosionOnDeath : NetworkBehaviour
    {
        static readonly SpawnPool<GameObject> _projectilePool = new SpawnPool<GameObject>();

        [SystemInitializer(typeof(ExpansionUtils))]
        static void Init()
        {
            _projectilePool.EnsureCapacity(3);
            _projectilePool.AddAssetEntry(AddressableGuids.RoR2_Base_Nullifier_NullifierDeathBombProjectile_prefab, new SpawnPoolEntryParameters(1f));
            _projectilePool.AddAssetEntry(AddressableGuids.RoR2_DLC1_VoidJailer_VoidJailerDeathBombProjectile_prefab, new SpawnPoolEntryParameters(0.3f, ExpansionUtils.DLC1));
            _projectilePool.AddAssetEntry(AddressableGuids.RoR2_DLC1_VoidMegaCrab_VoidMegaCrabDeathBombProjectile_prefab, new SpawnPoolEntryParameters(0.7f, ExpansionUtils.DLC1));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _projectilePool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        AssetOrDirectReference<GameObject>[] _voidProjectilePrefabRefByBodyIndex = [];

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            WeightedSelection<AssetOrDirectReference<GameObject>> projectileSelection = _projectilePool.GetSpawnSelection();

            _voidProjectilePrefabRefByBodyIndex = new AssetOrDirectReference<GameObject>[BodyCatalog.bodyCount];
            for (int i = 0; i < _voidProjectilePrefabRefByBodyIndex.Length; i++)
            {
                AssetOrDirectReference<GameObject> voidProjectilePrefabReference = projectileSelection.Evaluate(rng.nextNormalizedFloat);
                voidProjectilePrefabReference.LoadAsync();
                _voidProjectilePrefabRefByBodyIndex[i] = voidProjectilePrefabReference;
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                GlobalEventManager.onCharacterDeathGlobal += onCharacterDeathGlobal;
            }
        }

        void OnDestroy()
        {
            GlobalEventManager.onCharacterDeathGlobal -= onCharacterDeathGlobal;

            foreach (AssetOrDirectReference<GameObject> voidProjectilePrefabRef in _voidProjectilePrefabRefByBodyIndex)
            {
                voidProjectilePrefabRef?.Reset();
            }

            _voidProjectilePrefabRefByBodyIndex = [];
        }

        void onCharacterDeathGlobal(DamageReport report)
        {
            if (!NetworkServer.active)
                return;

            if (report.victimBodyIndex == BodyIndex.None)
                return;

            AssetOrDirectReference<GameObject> voidProjectileRef = ArrayUtils.GetSafe(_voidProjectilePrefabRefByBodyIndex, (int)report.victimBodyIndex);
            if (voidProjectileRef == null)
            {
                Log.Error($"Failed to select void implosion projectile for body death: {BodyCatalog.GetBodyName(report.victimBodyIndex)}");
                return;
            }

            Vector3 position = report.damageInfo.position;
            if (report.victimBody)
            {
                position = report.victimBody.corePosition;
            }
            else if (report.victimMaster && report.victimMaster.lostBodyToDeath)
            {
                position = report.victimMaster.deathFootPosition;
            }

            voidProjectileRef.CallOnLoaded(projectilePrefab =>
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = projectilePrefab,
                    position = position,
                    rotation = Quaternion.identity
                });
            });
        }

        [SerializedMember("p")]
        SerializedProjectileBodyPairing[] serializedProjectileBodyPairings
        {
            get
            {
                Dictionary<GameObject, List<BodyIndex>> bodyIndicesByProjectilePrefab = new Dictionary<GameObject, List<BodyIndex>>(_projectilePool.Count);

                for (int i = 0; i < _voidProjectilePrefabRefByBodyIndex.Length; i++)
                {
                    GameObject projectilePrefab = _voidProjectilePrefabRefByBodyIndex[i].WaitForAsset();
                    BodyIndex bodyIndex = (BodyIndex)i;

                    if (!bodyIndicesByProjectilePrefab.TryGetValue(projectilePrefab, out List<BodyIndex> bodyIndices))
                    {
                        bodyIndices = new List<BodyIndex>(_voidProjectilePrefabRefByBodyIndex.Length - i);
                        bodyIndicesByProjectilePrefab.Add(projectilePrefab, bodyIndices);
                    }

                    bodyIndices.Add(bodyIndex);
                }

                List<SerializedProjectileBodyPairing> pairings = new List<SerializedProjectileBodyPairing>(bodyIndicesByProjectilePrefab.Count);
                foreach (KeyValuePair<GameObject, List<BodyIndex>> kvp in bodyIndicesByProjectilePrefab)
                {
                    kvp.Deconstruct(out GameObject projectilePrefab, out List<BodyIndex> bodyIndices);

                    pairings.Add(new SerializedProjectileBodyPairing
                    {
                        ProjectileCatalogIndex = ProjectileCatalog.GetProjectileIndex(projectilePrefab),
                        BodyIncides = [.. bodyIndices]
                    });
                }

                return [.. pairings];
            }
            set
            {
                foreach (SerializedProjectileBodyPairing pairing in value)
                {
                    GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(pairing.ProjectileCatalogIndex);
                    if (!projectilePrefab)
                        continue;

                    foreach (BodyIndex bodyIndex in pairing.BodyIncides)
                    {
                        if (bodyIndex == BodyIndex.None)
                            continue;

                        if (!ArrayUtils.IsInBounds(_voidProjectilePrefabRefByBodyIndex, (int)bodyIndex))
                        {
                            Log.Error($"Body index out of bounds: {bodyIndex} (0-{_voidProjectilePrefabRefByBodyIndex.Length - 1})");
                            continue;
                        }

                        _voidProjectilePrefabRefByBodyIndex[(int)bodyIndex] = new AssetOrDirectReference<GameObject> { directRef = projectilePrefab };
                    }
                }
            }
        }

        [Serializable]
        class SerializedProjectileBodyPairing
        {
            [JsonConverter(typeof(ProjectileIndexConverter))]
            [JsonProperty("p")]
            public int ProjectileCatalogIndex;

            [JsonProperty("b")]
            public BodyIndex[] BodyIncides;
        }
    }
}
