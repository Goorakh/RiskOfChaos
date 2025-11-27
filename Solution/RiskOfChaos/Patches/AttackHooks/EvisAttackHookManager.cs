using EntityStates.Merc;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches.AttackHooks
{
    sealed class EvisAttackHookManager : AttackHookManager
    {
        readonly HurtBox _target;

        protected override AttackInfo AttackInfo { get; }

        public EvisAttackHookManager(Evis evis, HurtBox target)
        {
            AttackInfo = new AttackInfo(evis, target);
            _target = target;
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            if (!_target)
                return;

            CharacterBody attackerBody = null;
            if (attackInfo.Attacker)
            {
                attackerBody = attackInfo.Attacker.GetComponent<CharacterBody>();
            }

            float attackSpeed = 1f;

            if (attackerBody)
            {
                attackSpeed = attackerBody.attackSpeed;
            }

            float attackDuration = 1f / Evis.damageFrequency / attackSpeed;

            Util.PlayAttackSpeedSound(Evis.slashSoundString, attackInfo.Attacker, Evis.slashPitch);
            Util.PlaySound(Evis.dashSoundString, attackInfo.Attacker);
            Util.PlaySound(Evis.impactSoundString, attackInfo.Attacker);

            HurtBoxGroup hurtBoxGroup = _target.hurtBoxGroup;
            HurtBox hitTargetHurtBox = hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, hurtBoxGroup.hurtBoxes.Length - 1)];
            if (hitTargetHurtBox)
            {
                Vector3 impactPosition = hitTargetHurtBox.transform.position;
                Vector2 impactNormal = UnityEngine.Random.insideUnitCircle.normalized;
                Vector3 impactNormalHorizontal = new Vector3(impactNormal.x, 0f, impactNormal.y).normalized;

                EffectManager.SimpleImpactEffect(Evis.hitEffectPrefab, impactPosition, impactNormalHorizontal, false);
                Transform transform = _target.hurtBoxGroup.transform;
                TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(transform.gameObject);
                temporaryOverlayInstance.duration = attackDuration;
                temporaryOverlayInstance.animateShaderAlpha = true;
                temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlayInstance.destroyComponentOnEnd = true;
                temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEvisTarget");
                temporaryOverlayInstance.AddToCharacterModel(transform.GetComponent<CharacterModel>());
                if (NetworkServer.active)
                {
                    DamageInfo damageInfo = new DamageInfo();
                    attackInfo.PopulateDamageInfo(damageInfo);

                    hitTargetHurtBox.healthComponent.TakeDamage(damageInfo);
                    GlobalEventManager.instance.OnHitEnemy(damageInfo, hitTargetHurtBox.healthComponent.gameObject);
                    GlobalEventManager.instance.OnHitAll(damageInfo, hitTargetHurtBox.healthComponent.gameObject);
                }
            }
        }
    }
}
