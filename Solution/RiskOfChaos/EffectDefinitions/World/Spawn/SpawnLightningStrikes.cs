using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ConVar;
using RoR2.Orbs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

            static EffectIndex _orbEffectIndex = EffectIndex.Invalid;

            static GameObject _strikeEffect;

            [ContentInitializer]
            static IEnumerator LoadContent(EffectDefAssetCollection effectDefs)
            {
                List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(1);

                AsyncOperationHandle<GameObject> strikeEffectLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Lightning_LightningStrikeImpact_prefab, AsyncReferenceHandleUnloadType.Preload);
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

            [SystemInitializer(typeof(EffectCatalog))]
            static IEnumerator Init()
            {
                AsyncOperationHandle<GameObject> orbEffectPrefabLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Lightning_LightningStrikeOrbEffect_prefab, AsyncReferenceHandleUnloadType.OnSceneUnload);
                orbEffectPrefabLoad.OnSuccess(orbEffectPrefab =>
                {
                    _orbEffectIndex = EffectCatalog.FindEffectIndexFromPrefab(orbEffectPrefab);
                    if (_orbEffectIndex == EffectIndex.Invalid)
                    {
                        Log.Error($"Failed to find orb effect index from {orbEffectPrefab}");
                    }
                });

                yield return orbEffectPrefabLoad;
            }

            public float Force;

            GameObject _debugRangeIndicator;

            public override GameObject GetOrbEffect()
            {
                return EffectCatalog.GetEffectDef(_orbEffectIndex)?.prefab;
            }

            public override void Begin()
            {
                duration = 1f;
                scale = 6f;

                if (_orbEffectIndex != EffectIndex.Invalid)
                {
                    EffectManager.SpawnEffect(_orbEffectIndex, new EffectData
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

        float _strikeTargetsUpdateTimer = 0f;
        const float STRIKE_TARGETS_UPDATE_INTERVAL = 1f;

        readonly List<CharacterBody> _validStrikeTargets = [];

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

            _strikeTargetsUpdateTimer -= Time.fixedDeltaTime;
            if (_strikeTargetsUpdateTimer <= 0f)
            {
                _strikeTargetsUpdateTimer += STRIKE_TARGETS_UPDATE_INTERVAL;

                _validStrikeTargets.Clear();
                _validStrikeTargets.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);
                foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                {
                    if (body && (body.bodyFlags & CharacterBody.BodyFlags.Masterless) == 0 && body.healthComponent && body.healthComponent.alive)
                    {
                        _validStrikeTargets.Add(body);
                    }
                }
            }

            _lightningStrikeTimer -= Time.fixedDeltaTime;
            while (_lightningStrikeTimer <= 0f)
            {
                int groundNodeCount = 0;
                if (SceneInfo.instance && SceneInfo.instance.groundNodes)
                {
                    groundNodeCount = SceneInfo.instance.groundNodes.GetNodeCount();
                }

                int validTargetCount = groundNodeCount + _validStrikeTargets.Count;

                const float STRIKE_TIMER_MAX = 0.8f;

                _lightningStrikeTimer += 1f / Mathf.Max(1f / STRIKE_TIMER_MAX, validTargetCount / 35f);

                if (OrbManager.instance)
                {
                    Vector3 strikePosition = SpawnUtils.EvaluateToPosition(SpawnUtils.GetBestValidRandomPlacementRule(), _rng);
                    if (_validStrikeTargets.Count > 0 && _rng.nextNormalizedFloat <= Mathf.Min(0.1f, 0.005f * _validStrikeTargets.Count))
                    {
                        CharacterBody targetBody = _rng.NextElementUniform(_validStrikeTargets);
                        if (targetBody)
                        {
                            strikePosition = targetBody.corePosition;
                        }
                    }

                    OrbManager.instance.AddOrb(new StationaryLightningStrikeOrb
                    {
                        Force = 30f,
                        origin = strikePosition,
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
