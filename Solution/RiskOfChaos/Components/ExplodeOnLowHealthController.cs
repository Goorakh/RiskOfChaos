using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class ExplodeOnLowHealthController : NetworkBehaviour
    {
        static EffectIndex _explosionVFXEffectIndex = EffectIndex.Invalid;

        [SystemInitializer(typeof(EffectCatalogUtils))]
        static void Init()
        {
            _explosionVFXEffectIndex = EffectCatalogUtils.FindEffectIndex("VolatileBatteryExplosion");
            if (_explosionVFXEffectIndex == EffectIndex.Invalid)
            {
                Log.Error("Failed to find explosion vfx effect index");
            }
        }

        GenericOwnership _ownership;

        [SyncVar(hook = nameof(hookSetAttachedBodyObject))]
        GameObject _attachedBodyObject;

        bool _cachedIsPlayerControlled;
        float _cachedRadius;

        TeamIndex _cachedTeamIndex = TeamIndex.None;
        float _cachedFullHealth;
        GameObject _cachedAttacker;

        CharacterBody _attachedBody;
        public CharacterBody AttachedBody
        {
            get
            {
                return _attachedBody;
            }
            set
            {
                if (_attachedBody == value)
                    return;

                _attachedBody = value;
                _attachedBodyObject = _attachedBody ? _attachedBody.gameObject : null;

                if (_attachedBody)
                {
                    _cachedIsPlayerControlled = _attachedBody.isPlayerControlled;
                    _cachedRadius = _attachedBody.radius;
                }

                if (_ownership)
                {
                    _ownership.ownerObject = _attachedBodyObject;
                }
            }
        }

        void Awake()
        {
            _ownership = GetComponent<GenericOwnership>();
        }

        void FixedUpdate()
        {
            if (AttachedBody)
            {
                GameObject blastAttacker = AttachedBody.gameObject;

                HealthComponent healthComponent = AttachedBody.healthComponent;
                if (healthComponent)
                {
                    _cachedFullHealth = healthComponent.fullCombinedHealth;

                    // Credit whoever (probably) triggered the explosion, if applicable
                    if (healthComponent.lastHitAttacker)
                    {
                        blastAttacker = healthComponent.lastHitAttacker;
                    }
                }

                _cachedAttacker = blastAttacker;

                TeamComponent teamComponent = AttachedBody.teamComponent;
                if (teamComponent)
                {
                    _cachedTeamIndex = teamComponent.teamIndex;
                }
            }
        }

        void hookSetAttachedBodyObject(GameObject attachedBodyObject)
        {
            _attachedBodyObject = attachedBodyObject;
            AttachedBody = _attachedBodyObject ? _attachedBodyObject.GetComponent<CharacterBody>() : null;
        }

        public void Detonate()
        {
            bool isPlayerControlled = _cachedIsPlayerControlled;
            float radius = _cachedRadius;
            HealthComponent healthComponent = null;
            TeamComponent teamComponent = null;
            GameObject attacker = _cachedAttacker;
            if (AttachedBody)
            {
                isPlayerControlled = AttachedBody.isPlayerControlled;
                radius = AttachedBody.radius;
                healthComponent = AttachedBody.healthComponent;
                teamComponent = AttachedBody.teamComponent;
                attacker = AttachedBody.gameObject;
            }

            float fullHealth = _cachedFullHealth;
            if (healthComponent)
            {
                fullHealth = healthComponent.fullCombinedHealth;

                if (healthComponent.lastHitAttacker)
                {
                    attacker = healthComponent.lastHitAttacker;
                }
            }

            TeamIndex teamIndex = _cachedTeamIndex;
            if (teamComponent)
            {
                teamIndex = teamComponent.teamIndex;
            }

            float damageMultiplier = isPlayerControlled ? 3f : 1.25f;

            float damage = Mathf.Max(50f * Run.instance.teamlessDamageCoefficient, fullHealth * damageMultiplier);

            float blastRadius = Mathf.Max(radius * 1.5f, 20f);

            Vector3 blastCenter = transform.position;

            if (_explosionVFXEffectIndex != EffectIndex.Invalid)
            {
                EffectManager.SpawnEffect(_explosionVFXEffectIndex, new EffectData
                {
                    origin = blastCenter,
                    scale = blastRadius
                }, true);
            }

            DamageTypeCombo damageType = DamageTypeCombo.Generic;

            damageType.AddModdedDamageType(DamageTypes.BypassArmorSelf);
            damageType.AddModdedDamageType(DamageTypes.BypassBlockSelf);

            if (!isPlayerControlled)
            {
                damageType.AddModdedDamageType(DamageTypes.BypassOSPSelf);
                damageType.AddModdedDamageType(DamageTypes.NonLethalToPlayers);
            }

            BlastAttack blastAttack = new BlastAttack
            {
                position = blastCenter,
                radius = blastRadius,
                falloffModel = BlastAttack.FalloffModel.Linear,
                attacker = attacker,
                inflictor = gameObject,
                damageColorIndex = DamageColorIndex.Item,
                baseDamage = damage,
                baseForce = 5000f,
                attackerFiltering = AttackerFiltering.AlwaysHit,
                crit = false,
                procCoefficient = 0f,
                teamIndex = teamIndex,
                damageType = damageType
            };

            blastAttack.Fire();
        }
    }
}
