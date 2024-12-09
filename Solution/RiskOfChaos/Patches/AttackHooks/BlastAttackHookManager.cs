using HG;
using RiskOfChaos.Utilities;
using RiskOfChaos_PatcherInterop;
using RoR2;
using RoR2.Projectile;

namespace RiskOfChaos.Patches.AttackHooks
{
    class BlastAttackHookManager : AttackHookManager
    {
        readonly BlastAttack _blastAttack;

        public BlastAttackHookManager(BlastAttack blastAttack)
        {
            _blastAttack = AttackUtils.Clone(blastAttack);
        }

        protected override void fireAttackCopy()
        {
            AttackUtils.Clone(_blastAttack).Fire();
        }

        protected override bool setupProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            fireProjectileInfo.position = _blastAttack.position;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(UnityEngine.Random.onUnitSphere);
            fireProjectileInfo.owner = _blastAttack.attacker;
            fireProjectileInfo.damage = _blastAttack.baseDamage;
            fireProjectileInfo.force = _blastAttack.baseForce;
            fireProjectileInfo.crit = _blastAttack.crit;
            fireProjectileInfo.damageColorIndex = _blastAttack.damageColorIndex;
            fireProjectileInfo.procChainMask = _blastAttack.procChainMask;
            fireProjectileInfo.damageTypeOverride = _blastAttack.damageType;
            fireProjectileInfo.SetProcCoefficientOverride(_blastAttack.procCoefficient);
            return true;
        }
    }
}
