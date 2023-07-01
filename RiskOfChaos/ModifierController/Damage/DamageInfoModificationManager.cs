using RoR2;
using System;
using System.Reflection;

namespace RiskOfChaos.ModifierController.Damage
{
    public class DamageInfoModificationManager : ValueModificationManager<IDamageInfoModificationProvider, DamageInfo>
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
            if (!Instance || damageInfo == null)
                return;

            Type damageInfoType = damageInfo.GetType();

            DamageInfo damageInfoCopy;
            try
            {
                damageInfoCopy = (DamageInfo)Activator.CreateInstance(damageInfoType);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to create instance of DamageInfo type {damageInfoType.FullName}: {ex}");
                return;
            }

            foreach (FieldInfo field in damageInfoType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    field.SetValue(damageInfoCopy, field.GetValue(damageInfo));
                }
                catch (Exception ex)
                {
                    Log.Warning_NoCallerPrefix($"Failed to set copy field value {field.DeclaringType.FullName}.{field.Name} ({field.FieldType.FullName}): {ex}");
                }
            }

            damageInfo = Instance.getModifiedValue(damageInfoCopy);
        }

        protected override void updateValueModifications()
        {
        }
    }
}
