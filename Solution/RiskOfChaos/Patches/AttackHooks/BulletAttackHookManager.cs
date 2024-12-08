using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Reflection;
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
    }
}
