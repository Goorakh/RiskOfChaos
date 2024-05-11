using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.ConVar;
using RoR2.Orbs;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_lightning_strikes", 30f)]
    public sealed class SpawnLightningStrikes : TimedEffect
    {
        class StationaryLightningStrikeOrb : GenericDamageOrb
        {
            static readonly GameObject _orbEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeOrbEffect.prefab").WaitForCompletion();

            static readonly GameObject _strikeEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeImpact.prefab").WaitForCompletion();

            static readonly BoolConVar _enableDebugRangeIndicator = new BoolConVar("roc_lightning_strikes_debug_range_indicator", ConVarFlags.None, "0", "Enables debug visualization of the \"Risk of Thunder\" lightning strikes damage radius");

            GameObject _debugRangeIndicator;

            public override void Begin()
            {
                base.Begin();

                duration = 1f;
                scale = 6f;

                if (_orbEffect)
                {
                    EffectManager.SpawnEffect(_orbEffect, new EffectData
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
                    baseDamage = 50f * Run.instance.teamlessDamageCoefficient,
                    baseForce = 0f,
                    bonusForce = new Vector3(0f, 10f, 0f),
                    damageColorIndex = DamageColorIndex.Item,
                    damageType = DamageType.SlowOnHit,
                    falloffModel = BlastAttack.FalloffModel.None,
                    position = origin,
                    procCoefficient = 0f,
                    radius = scale,
                    teamIndex = TeamIndex.None
                }.Fire();

                if (_debugRangeIndicator)
                {
                    GameObject.Destroy(_debugRangeIndicator);
                }
            }
        }

        public override void OnStart()
        {
            RoR2Application.onFixedUpdate += fixedUpdate;
        }

        const float TIME_BETWEEN_LIGHTNING_STRIKES = 0.25f;
        float _lastLightningStrikeTime = float.NegativeInfinity;

        void fixedUpdate()
        {
            if (TimeElapsed > _lastLightningStrikeTime + TIME_BETWEEN_LIGHTNING_STRIKES)
            {
                if (OrbManager.instance)
                {
                    CharacterBody[] validStrikeTargets = CharacterBody.readOnlyInstancesList.Where(c =>
                    {
                        return c.healthComponent && c.healthComponent.alive && (c.bodyFlags & CharacterBody.BodyFlags.Masterless) == 0;
                    }).ToArray();

                    Vector3 origin;
                    if (validStrikeTargets.Length > 0 && RNG.nextNormalizedFloat < Mathf.Min(0.1f, 0.0025f * validStrikeTargets.Length))
                    {
                        origin = RNG.NextElementUniform(validStrikeTargets).footPosition;
                    }
                    else
                    {
                        origin = SpawnUtils.EvaluateToPosition(SpawnUtils.GetBestValidRandomPlacementRule(), RNG);
                    }

                    OrbManager.instance.AddOrb(new StationaryLightningStrikeOrb
                    {
                        origin = origin
                    });
                }

                _lastLightningStrikeTime += TIME_BETWEEN_LIGHTNING_STRIKES;
            }
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= fixedUpdate;
        }
    }
}
