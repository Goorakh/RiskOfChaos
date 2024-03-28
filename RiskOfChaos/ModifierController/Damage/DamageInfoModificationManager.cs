using MonoMod.Utils;
using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Damage
{
    [ValueModificationManager]
    public class DamageInfoModificationManager : ValueModificationManager<DamageInfo>
    {
        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.GlobalEventManager.OnHitAll += (orig, self, damageInfo, hitObject) =>
            {
                tryModifyDamageInfo(ref damageInfo);
                orig(self, damageInfo, hitObject);
            };

            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                tryModifyDamageInfo(ref damageInfo);
                orig(self, damageInfo, victim);
            };

            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                tryModifyDamageInfo(ref damageInfo);
                orig(self, damageInfo);
            };

            _hasAppliedPatches = true;
        }

        static DamageInfoModificationManager _instance;
        public static DamageInfoModificationManager Instance => _instance;

        static readonly WeakReference _lastModifiedDamageInfoReference = new WeakReference(null);

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);
            tryApplyPatches();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);
        }

        static void tryModifyDamageInfo(ref DamageInfo damageInfo)
        {
            if (!NetworkServer.active || !Instance || !Instance.AnyModificationActive || damageInfo == null)
                return;

            if (_lastModifiedDamageInfoReference.SafeGetIsAlive() && ReferenceEquals(_lastModifiedDamageInfoReference.SafeGetTarget(), damageInfo))
                return;

            DamageInfo modifiedDamageInfo = Instance.GetModifiedValue(damageInfo);

            if (!ReferenceEquals(modifiedDamageInfo, damageInfo))
            {
                Log.Error("Modification changed DamageInfo instance");
            }

            _lastModifiedDamageInfoReference.Target = damageInfo;
        }

        public override DamageInfo InterpolateValue(in DamageInfo a, in DamageInfo b, float t)
        {
            throw new NotSupportedException("DamageInfo interpolation is not supported");
        }

        public override void UpdateValueModifications()
        {
        }
    }
}
