using RiskOfChaos.Utilities;
using RiskOfChaos_PatcherInterop;
using RoR2;
using RoR2.Projectile;
using System;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    class BulletAttackHookManager : AttackHookManager
    {
        public enum FireType
        {
            Multi,
            Single,
            Single_ReturnHit
        }

        readonly BulletAttack _bulletAttack;
        readonly BulletAttack _bulletAttackTemplate;
        readonly Vector3 _normal;
        readonly int _muzzleIndex;
        readonly FireType _fireType;

        public BulletAttackHookManager(BulletAttack bulletAttack, Vector3 normal, int muzzleIndex, FireType fireType)
        {
            _bulletAttack = bulletAttack;
            _bulletAttackTemplate = AttackUtils.Clone(bulletAttack);
            _normal = normal;
            _muzzleIndex = muzzleIndex;
            _fireType = fireType;
        }

        protected override void fireAttackCopy()
        {
            BulletAttack bulletAttackCopy = AttackUtils.Clone(_bulletAttackTemplate);
            switch (_fireType)
            {
                case FireType.Multi:
                    bulletAttackCopy.FireMulti(_normal, _muzzleIndex);
                    break;
                case FireType.Single:
                    bulletAttackCopy.FireSingle(_normal, _muzzleIndex);
                    break;
                case FireType.Single_ReturnHit:
                    bulletAttackCopy.FireSingle_ReturnHit(_normal, _muzzleIndex);
                    break;
                default:
                    throw new NotImplementedException($"Fire type {_fireType} is not implemented");
            }
        }

        protected override bool tryFireBounce(AttackHookMask activeAttackHooks)
        {
            return BulletBounceHook.TryStartBounce(_bulletAttack, _normal, _muzzleIndex, activeAttackHooks);
        }

        protected override bool setupProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            Vector3 position = _bulletAttack.origin;

            if (_bulletAttack.weapon && _bulletAttack.weapon.TryGetComponent(out ModelLocator weaponModelLocator))
            {
                Transform weaponModelTransform = weaponModelLocator.modelTransform;
                if (weaponModelTransform && weaponModelTransform.TryGetComponent(out ChildLocator weaponChildLocator))
                {
                    Transform muzzleTransform = weaponChildLocator.FindChild(_muzzleIndex);
                    if (muzzleTransform)
                    {
                        position = muzzleTransform.position;
                    }
                }
            }

            fireProjectileInfo.position = position;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(_normal);
            fireProjectileInfo.owner = _bulletAttack.owner;
            fireProjectileInfo.damage = _bulletAttack.damage;
            fireProjectileInfo.force = _bulletAttack.force;
            fireProjectileInfo.crit = _bulletAttack.isCrit;
            fireProjectileInfo.damageColorIndex = _bulletAttack.damageColorIndex;
            fireProjectileInfo.procChainMask = _bulletAttack.procChainMask;
            fireProjectileInfo.damageTypeOverride = _bulletAttack.damageType;

            fireProjectileInfo.SetProcCoefficientOverride(_bulletAttack.procCoefficient);

            return true;
        }
    }
}
