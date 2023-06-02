using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_lightning_strikes")]
    [ChaosTimedEffect(30f)]
    public sealed class SpawnLightningStrikes : TimedEffect
    {
        class StationaryLightningStrikeOrb : GenericDamageOrb
        {
            static readonly GameObject _orbEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeOrbEffect.prefab").WaitForCompletion();

            static readonly GameObject _strikeEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeImpact.prefab").WaitForCompletion();

            public override void Begin()
            {
                base.Begin();

                duration = 1f;
                if (_orbEffect)
                {
                    EffectManager.SpawnEffect(_orbEffect, new EffectData
                    {
                        scale = scale,
                        origin = origin,
                        genericFloat = duration
                    }, true);
                }
            }

            public override void OnArrival()
            {
                base.OnArrival();

                const float RADIUS = 3f;

                EffectManager.SpawnEffect(_strikeEffect, new EffectData
                {
                    origin = origin,
                    scale = RADIUS / 3f,
                }, true);

                new BlastAttack
                {
                    baseDamage = 100f,
                    baseForce = 0f,
                    bonusForce = new Vector3(0f, 10f, 0f),
                    damageColorIndex = DamageColorIndex.Item,
                    damageType = DamageType.SlowOnHit | DamageType.Stun1s | DamageType.Shock5s,
                    falloffModel = BlastAttack.FalloffModel.None,
                    position = origin,
                    procCoefficient = 0f,
                    radius = RADIUS,
                    teamIndex = TeamIndex.None
                }.Fire();
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
                    OrbManager.instance.AddOrb(new StationaryLightningStrikeOrb
                    {
                        origin = SpawnUtils.EvaluateToPosition(SpawnUtils.GetBestValidRandomPlacementRule(), RNG),
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
