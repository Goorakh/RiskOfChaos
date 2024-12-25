using RiskOfChaos.Utilities;
using RoR2;
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

        protected override AttackInfo AttackInfo { get; }

        public BulletAttackHookManager(BulletAttack bulletAttack, Vector3 normal, int muzzleIndex, FireType fireType)
        {
            _bulletAttack = bulletAttack;
            _bulletAttackTemplate = AttackUtils.Clone(bulletAttack);
            _normal = normal;
            _muzzleIndex = muzzleIndex;
            _fireType = fireType;

            AttackInfo = new AttackInfo(_bulletAttack, normal, muzzleIndex);
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            BulletAttack bulletAttackCopy = AttackUtils.Clone(_bulletAttackTemplate);
            attackInfo.PopulateBulletAttack(bulletAttackCopy);
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

        protected override bool tryFireBounce()
        {
            return BulletBounceHook.TryStartBounce(_bulletAttack, AttackInfo);
        }
    }
}
