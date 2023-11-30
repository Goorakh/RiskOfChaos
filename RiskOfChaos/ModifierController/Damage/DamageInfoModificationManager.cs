using RiskOfChaos.ModCompatibility;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Reflection;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Damage
{
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

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);
            tryApplyPatches();
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }

        static void tryModifyDamageInfo(ref DamageInfo damageInfo)
        {
            if (!NetworkServer.active || !Instance || !Instance.AnyModificationActive || damageInfo == null)
                return;

            DamageInfo damageInfoCopy;
            try
            {
                damageInfoCopy = damageInfo.ShallowCopy(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to create shallow copy of {damageInfo}: {ex}");
                return;
            }

            if (DamageAPICompat.Active)
            {
                DamageAPICompat.CopyModdedDamageTypes(damageInfo, damageInfoCopy);
            }

            damageInfo = Instance.getModifiedValue(damageInfoCopy);
        }

        protected override DamageInfo interpolateValue(in DamageInfo a, in DamageInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return b;
        }

        protected override void updateValueModifications()
        {
        }
    }
}
