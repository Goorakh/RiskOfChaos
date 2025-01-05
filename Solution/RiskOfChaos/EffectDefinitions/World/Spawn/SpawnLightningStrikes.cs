using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ConVar;
using RoR2.Orbs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_lightning_strikes", 30f)]
    public sealed class SpawnLightningStrikes : NetworkBehaviour
    {
        class StationaryLightningStrikeOrb : GenericDamageOrb
        {
            static readonly BoolConVar _enableDebugRangeIndicator = new BoolConVar("roc_lightning_strikes_debug_range_indicator", ConVarFlags.None, "0", "Enables debug visualization of the \"Risk of Thunder\" lightning strikes damage radius");

            static GameObject _orbEffect;
            static GameObject _strikeEffect;

            [ContentInitializer]
            static IEnumerator LoadContent(EffectDefAssetCollection effectDefs)
            {
                List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(1);

                AsyncOperationHandle<GameObject> strikeEffectLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeImpact.prefab");
                strikeEffectLoad.OnSuccess(strikeEffectPrefab =>
                {
                    GameObject prefab = strikeEffectPrefab.InstantiatePrefab("LightningStrikeImpact_SoundFixed");

                    EffectComponent effectComponent = prefab.GetComponent<EffectComponent>();
                    effectComponent.soundName = "Play_item_use_lighningArm";

                    effectDefs.Add(new EffectDef(prefab));
                    _strikeEffect = prefab;
                });

                asyncOperations.Add(strikeEffectLoad);

                yield return asyncOperations.WaitForAllLoaded();
            }

            [SystemInitializer]
            static void Init()
            {
                AsyncOperationHandle<GameObject> orbEffectLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeOrbEffect.prefab");
                orbEffectLoad.OnSuccess(orbEffectPrefab => _orbEffect = orbEffectPrefab);
            }

            public float Force;

            GameObject _debugRangeIndicator;

            public override GameObject GetOrbEffect()
            {
                return _orbEffect;
            }

            public override void Begin()
            {
                duration = 1f;
                scale = 6f;

                GameObject orbEffect = GetOrbEffect();
                if (orbEffect)
                {
                    EffectManager.SpawnEffect(orbEffect, new EffectData
                    {
                        scale = scale / 3f,
                        origin = origin,
                        genericFloat = duration
                    }, true);
                }

                if (_enableDebugRangeIndicator.value)
                {
                    _debugRangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _debugRangeIndicator.GetComponent<Renderer>().material.color = Color.red;
                    _debugRangeIndicator.GetComponent<Collider>().enabled = false;
                    _debugRangeIndicator.transform.position = origin;
                    _debugRangeIndicator.transform.localScale = Vector3.one * scale;
                }
            }

            public override void OnArrival()
            {
                base.OnArrival();

                if (_strikeEffect)
                {
                    EffectManager.SpawnEffect(_strikeEffect, new EffectData
                    {
                        origin = origin,
                        scale = scale / 3f,
                    }, true);
                }

                new BlastAttack
                {
                    baseDamage = damageValue,
                    baseForce = Force,
                    damageColorIndex = damageColorIndex,
                    damageType = damageType,
                    falloffModel = BlastAttack.FalloffModel.None,
                    position = origin,
                    procCoefficient = procCoefficient,
                    radius = scale,
                    teamIndex = teamIndex
                }.Fire();

                if (_debugRangeIndicator)
                {
                    Destroy(_debugRangeIndicator);
                }
            }
        }

        ChaosEffectComponent _effectComponent;

        [SerializedMember("rng")]
        Xoroshiro128Plus _rng;

        float _lightningStrikeTimer = 0f;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;

            _lightningStrikeTimer -= Time.fixedDeltaTime;
            while (_lightningStrikeTimer <= 0f)
            {
                List<CharacterBody> validStrikeTargets = new List<CharacterBody>(CharacterBody.readOnlyInstancesList.Count);
                foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                {
                    if (body && (body.bodyFlags & CharacterBody.BodyFlags.Masterless) == 0 && body.healthComponent && body.healthComponent.alive)
                    {
                        validStrikeTargets.Add(body);
                    }
                }

                int groundNodeCount = 0;
                if (SceneInfo.instance && SceneInfo.instance.groundNodes)
                {
                    groundNodeCount = SceneInfo.instance.groundNodes.GetNodeCount();
                }

                const float STRIKE_TIMER_MAX = 0.8f;

                _lightningStrikeTimer += 1f / Mathf.Max(1f / STRIKE_TIMER_MAX, (groundNodeCount + validStrikeTargets.Count) / 35f);

                if (OrbManager.instance)
                {
                    Vector3 origin;
                    if (validStrikeTargets.Count > 0 && _rng.nextNormalizedFloat < Mathf.Min(0.1f, 0.0025f * validStrikeTargets.Count))
                    {
                        origin = _rng.NextElementUniform(validStrikeTargets).footPosition;
                    }
                    else
                    {
                        origin = SpawnUtils.EvaluateToPosition(SpawnUtils.GetBestValidRandomPlacementRule(), _rng);
                    }

                    OrbManager.instance.AddOrb(new StationaryLightningStrikeOrb
                    {
                        Force = 30f,
                        origin = origin,
                        damageValue = 50f * Run.instance.teamlessDamageCoefficient,
                        damageColorIndex = DamageColorIndex.Item,
                        damageType = DamageType.SlowOnHit,
                        procCoefficient = 0f,
                        teamIndex = TeamIndex.None
                    });
                }
            }
        }
    }
}
