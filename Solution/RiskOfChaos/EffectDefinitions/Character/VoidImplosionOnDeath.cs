using HG;
using Newtonsoft.Json;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Serialization.Converters;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("void_implosion_on_death", 60f, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility([
        "Effect: Void Implosion on Death"
    ])]
    public sealed class VoidImplosionOnDeath : NetworkBehaviour
    {
        readonly record struct VoidImplosionProjectileInfo(GameObject ProjectilePrefab);

        static readonly WeightedSelection<VoidImplosionProjectileInfo> _projectileSelection = new WeightedSelection<VoidImplosionProjectileInfo>();

        [SystemInitializer]
        static IEnumerator Init()
        {
            AsyncOperationHandle<GameObject> nullifierImplosionLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab");
            AsyncOperationHandle<GameObject> jailerImplosionLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidJailer/VoidJailerDeathBombProjectile.prefab");
            AsyncOperationHandle<GameObject> devastatorImplosionLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombProjectile.prefab");

            yield return AsyncOperationExtensions.WaitForAllLoaded([nullifierImplosionLoad, jailerImplosionLoad, devastatorImplosionLoad]);
            
            static void addChoice(AsyncOperationHandle<GameObject> prefabLoad, float weight)
            {
                if (!prefabLoad.Result)
                {
                    Log.Error($"Failed to load prefab {prefabLoad.LocationName}");
                    return;
                }

                _projectileSelection.AddChoice(new VoidImplosionProjectileInfo(prefabLoad.Result), weight);
            }

            addChoice(nullifierImplosionLoad, 1f);
            addChoice(jailerImplosionLoad, 0.3f);
            addChoice(devastatorImplosionLoad, 0.7f);
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
                _voidProjectilePrefabByBodyIndex[i] = _projectileSelection.Evaluate(rng.nextNormalizedFloat).ProjectilePrefab;
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

            int index = (int)report.victimBodyIndex;
            if (!ArrayUtils.IsInBounds(_voidProjectilePrefabByBodyIndex, index))
                return;

            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = _voidProjectilePrefabByBodyIndex[index],
                position = report.victimBody.corePosition,
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

                SerializedProjectileBodyPairing[] pairings = new SerializedProjectileBodyPairing[bodyIndicesByProjectilePrefab.Count];
                int currentIndex = 0;
                foreach (KeyValuePair<GameObject, List<BodyIndex>> kvp in bodyIndicesByProjectilePrefab)
                {
                    GameObject projectilePrefab = kvp.Key;
                    List<BodyIndex> bodyIndices = kvp.Value;

                    pairings[currentIndex] = new SerializedProjectileBodyPairing
                    {
                        ProjectileCatalogIndex = ProjectileCatalog.GetProjectileIndex(projectilePrefab),
                        BodyIncides = [.. bodyIndices]
                    };

                    currentIndex++;
                }

                return pairings;
            }
            set
            {
                foreach (SerializedProjectileBodyPairing pairing in value)
                {
                    GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(pairing.ProjectileCatalogIndex);
                    foreach (BodyIndex bodyIndex in pairing.BodyIncides)
                    {
                        if (bodyIndex == BodyIndex.None)
                            continue;

                        if (!ArrayUtils.IsInBounds(_voidProjectilePrefabByBodyIndex, (int)bodyIndex))
                        {
                            Log.Error($"Body index out of bounds: {bodyIndex} (0-{_voidProjectilePrefabByBodyIndex.Length})");
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
