using HG;
using Newtonsoft.Json;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Serialization.Converters;
using RiskOfChaos.Utilities;
using RoR2;
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
        static GameObject _fallbackProjectilePrefab;

        static readonly SpawnPool<GameObject> _projectileSelection = new SpawnPool<GameObject>();

        [SystemInitializer(typeof(ExpansionUtils), typeof(ProjectileCatalog))]
        static void Init()
        {
            _projectileSelection.EnsureCapacity(3);

            GameObject nullifierImplosionPrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex("NullifierDeathBombProjectile"));
            if (!nullifierImplosionPrefab)
            {
                Log.Error("Failed to find nullifier implosion projectile prefab");
            }

            GameObject jailerImplosionPrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex("VoidJailerDeathBombProjectile"));
            if (!jailerImplosionPrefab)
            {
                Log.Error("Failed to find jailer implosion projectile prefab");
            }

            GameObject devastatorImplosionPrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex("VoidMegaCrabDeathBombProjectile"));
            if (!devastatorImplosionPrefab)
            {
                Log.Error("Failed to find devastator implosion projectile prefab");
            }

            if (nullifierImplosionPrefab)
            {
                _projectileSelection.AddEntry(new SpawnPool<GameObject>.Entry(nullifierImplosionPrefab, new SpawnPoolEntryParameters(1f)));
            }

            if (jailerImplosionPrefab)
            {
                _projectileSelection.AddEntry(new SpawnPool<GameObject>.Entry(jailerImplosionPrefab, new SpawnPoolEntryParameters(0.3f, ExpansionUtils.DLC1)));
            }

            if (devastatorImplosionPrefab)
            {
                _projectileSelection.AddEntry(new SpawnPool<GameObject>.Entry(devastatorImplosionPrefab, new SpawnPoolEntryParameters(0.7f, ExpansionUtils.DLC1)));
            }

            _projectileSelection.TrimExcess();

            _fallbackProjectilePrefab = nullifierImplosionPrefab;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _projectileSelection.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        GameObject[] _voidProjectilePrefabByBodyIndex = [];

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _voidProjectilePrefabByBodyIndex = new GameObject[BodyCatalog.bodyCount];
            for (int i = 0; i < _voidProjectilePrefabByBodyIndex.Length; i++)
            {
                _voidProjectilePrefabByBodyIndex[i] = _projectileSelection.PickRandomEntry(rng);
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            }
        }

        void OnDestroy()
        {
            GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
        }

        void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            if (!NetworkServer.active || !ProjectileManager.instance)
                return;

            if (report.victimBodyIndex == BodyIndex.None)
                return;

            GameObject projectilePrefab = ArrayUtils.GetSafe(_voidProjectilePrefabByBodyIndex, (int)report.victimBodyIndex);
            if (!projectilePrefab)
            {
                projectilePrefab = _fallbackProjectilePrefab;
            }

            if (!projectilePrefab)
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

            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = projectilePrefab,
                position = position,
                rotation = Quaternion.identity
            });
        }

        [SerializedMember("p")]
        SerializedProjectileBodyPairing[] serializedProjectileBodyPairings
        {
            get
            {
                Dictionary<GameObject, List<BodyIndex>> bodyIndicesByProjectilePrefab = new Dictionary<GameObject, List<BodyIndex>>(_projectileSelection.Count);

                for (int i = 0; i < _voidProjectilePrefabByBodyIndex.Length; i++)
                {
                    GameObject projectilePrefab = _voidProjectilePrefabByBodyIndex[i];
                    BodyIndex bodyIndex = (BodyIndex)i;

                    if (!bodyIndicesByProjectilePrefab.TryGetValue(projectilePrefab, out List<BodyIndex> bodyIndices))
                    {
                        bodyIndices = new List<BodyIndex>(_voidProjectilePrefabByBodyIndex.Length - i);
                        bodyIndicesByProjectilePrefab.Add(projectilePrefab, bodyIndices);
                    }

                    bodyIndices.Add(bodyIndex);
                }

                List<SerializedProjectileBodyPairing> pairings = new List<SerializedProjectileBodyPairing>(bodyIndicesByProjectilePrefab.Count);
                foreach (KeyValuePair<GameObject, List<BodyIndex>> kvp in bodyIndicesByProjectilePrefab)
                {
                    GameObject projectilePrefab = kvp.Key;
                    List<BodyIndex> bodyIndices = kvp.Value;

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
                    {
                        projectilePrefab = _fallbackProjectilePrefab;
                    }

                    foreach (BodyIndex bodyIndex in pairing.BodyIncides)
                    {
                        if (bodyIndex == BodyIndex.None)
                            continue;

                        if (!ArrayUtils.IsInBounds(_voidProjectilePrefabByBodyIndex, (int)bodyIndex))
                        {
                            Log.Error($"Body index out of bounds: {bodyIndex} (0-{_voidProjectilePrefabByBodyIndex.Length - 1})");
                            continue;
                        }

                        _voidProjectilePrefabByBodyIndex[(int)bodyIndex] = projectilePrefab;
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
