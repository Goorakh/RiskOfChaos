using RiskOfChaos.Collections;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities.Reflection;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class OrbExtensions
    {
        class CachedOrbFields
        {
            public readonly Type OrbType;

            public readonly FieldWrapper<CharacterBody, Orb> Attacker;
            public readonly FieldWrapper<ProcChainMask, Orb> ProcChainMask;
            public readonly FieldWrapper<TeamIndex, Orb> TeamIndex;
            public readonly FieldWrapper<List<HealthComponent>, Orb> BouncedObjects;
            public readonly FieldWrapper<int, Orb> BouncesRemaining;
            public readonly FieldWrapper<float, Orb> DamageValue;
            public readonly FieldWrapper<float, Orb> ForceScalar;
            public readonly FieldWrapper<bool, Orb> IsCrit;
            public readonly FieldWrapper<DamageColorIndex, Orb> DamageColorIndex;
            public readonly FieldWrapper<DamageTypeCombo, Orb> DamageType;
            public readonly FieldWrapper<float, Orb> ProcCoefficient;

            public CachedOrbFields(Type orbType)
            {
                OrbType = orbType;

                Attacker = new FieldWrapper<CharacterBody, Orb>(new CachedFieldReference(OrbType, "attacker", BindingFlags.Instance | BindingFlags.Public))
                {
                    ConvertSetValue = (body, targetType) =>
                    {
                        if (targetType == typeof(GameObject))
                        {
                            return body.gameObject;
                        }
                        else
                        {
                            throw new NotImplementedException($"Target type {targetType.FullName} is not implemented");
                        }
                    },
                    ConvertGetValue = (obj, fieldType) =>
                    {
                        if (fieldType == typeof(GameObject))
                        {
                            GameObject gameObject = obj as GameObject;
                            return gameObject ? gameObject.GetComponent<CharacterBody>() : null;
                        }
                        else
                        {
                            throw new NotImplementedException($"Field type {fieldType.FullName} is not implemented");
                        }
                    }
                };

                ProcChainMask = new FieldWrapper<ProcChainMask, Orb>(new CachedFieldReference(OrbType, "procChainMask", typeof(ProcChainMask), BindingFlags.Instance | BindingFlags.Public));

                TeamIndex = new FieldWrapper<TeamIndex, Orb>(new CachedFieldReference(OrbType, typeof(TeamIndex), BindingFlags.Instance | BindingFlags.Public));

                BouncedObjects = new FieldWrapper<List<HealthComponent>, Orb>(new CachedFieldReference(OrbType, "bouncedObjects", typeof(List<HealthComponent>), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

                BouncesRemaining = new FieldWrapper<int, Orb>(new CachedFieldReference(OrbType, "bouncesRemaining", typeof(int), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

                DamageValue = new FieldWrapper<float, Orb>(new CachedFieldReference(OrbType, new Regex(@"^(base)?damage(value)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), typeof(float), BindingFlags.Instance | BindingFlags.Public));

                ForceScalar = new FieldWrapper<float, Orb>(new CachedFieldReference(OrbType, new Regex(@"^force(scalar)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), typeof(float), BindingFlags.Instance | BindingFlags.Public));

                IsCrit = new FieldWrapper<bool, Orb>(new CachedFieldReference(OrbType, new Regex(@"^(is)?crit$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), typeof(bool), BindingFlags.Instance | BindingFlags.Public));

                DamageColorIndex = new FieldWrapper<DamageColorIndex, Orb>(new CachedFieldReference(OrbType, typeof(DamageColorIndex), BindingFlags.Instance | BindingFlags.Public));

                DamageType = new FieldWrapper<DamageTypeCombo, Orb>(new CachedFieldReference(OrbType, typeof(DamageTypeCombo), BindingFlags.Instance | BindingFlags.Public));

                ProcCoefficient = new FieldWrapper<float, Orb>(new CachedFieldReference(OrbType, "procCoefficient", typeof(float), BindingFlags.Instance | BindingFlags.Public));
            }
        }

        static readonly Dictionary<Type, CachedOrbFields> _cachedOrbFields = [];

        static CachedOrbFields getOrCreateOrbFields(Type orbType)
        {
            if (_cachedOrbFields.TryGetValue(orbType, out CachedOrbFields orbFields))
                return orbFields;

            orbFields = new CachedOrbFields(orbType);
            _cachedOrbFields.Add(orbType, orbFields);
            return orbFields;
        }

        public static CharacterBody GetAttacker(this Orb orb)
        {
            if (orb is null)
                return null;

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.Attacker.TryGet(orb, out CharacterBody attacker) ? attacker : null;
        }

        public static bool TryGetProcChainMask(this Orb orb, out ProcChainMask procChainMask)
        {
            if (orb is null)
            {
                procChainMask = default;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.ProcChainMask.TryGet(orb, out procChainMask);
        }

        public static bool TrySetProcChainMask(this Orb orb, ProcChainMask procChainMask)
        {
            if (orb is null)
                return false;

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.ProcChainMask.TrySet(orb, procChainMask);
        }

        public static bool TryGetTeamIndex(this Orb orb, out TeamIndex teamIndex)
        {
            if (orb is null)
            {
                teamIndex = TeamIndex.None;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.TeamIndex.TryGet(orb, out teamIndex);
        }

        public static bool TryGetBouncedObjects(this Orb orb, out ReadOnlyCollection<HealthComponent> bouncedObjects)
        {
            if (orb is null)
            {
                bouncedObjects = Empty<HealthComponent>.ReadOnlyCollection;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());

            if (!orbFields.BouncedObjects.TryGet(orb, out List<HealthComponent> bouncedObjectsList))
            {
                bouncedObjects = Empty<HealthComponent>.ReadOnlyCollection;
                return false;
            }

            bouncedObjects = bouncedObjectsList?.AsReadOnly() ?? Empty<HealthComponent>.ReadOnlyCollection;
            return true;
        }

        public static bool TryGetBouncesRemaining(this Orb orb, out int bouncesRemaining)
        {
            if (orb is null)
            {
                bouncesRemaining = 0;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.BouncesRemaining.TryGet(orb, out bouncesRemaining);
        }

        public static bool TrySetBouncesRemaining(this Orb orb, int bouncesRemaining)
        {
            if (orb is null)
                return false;

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.BouncesRemaining.TrySet(orb, bouncesRemaining);
        }

        public static bool TryGetDamageValue(this Orb orb, out float damage)
        {
            if (orb is null)
            {
                damage = -1f;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.DamageValue.TryGet(orb, out damage);
        }

        public static bool TryGetForceScalar(this Orb orb, out float forceScalar)
        {
            if (orb is null)
            {
                forceScalar = -1f;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.ForceScalar.TryGet(orb, out forceScalar);
        }

        public static bool TryGetIsCrit(this Orb orb, out bool isCrit)
        {
            if (orb is null)
            {
                isCrit = false;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.IsCrit.TryGet(orb, out isCrit);
        }

        public static bool TryGetDamageColorIndex(this Orb orb, out DamageColorIndex damageColorIndex)
        {
            if (orb is null)
            {
                damageColorIndex = default;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.DamageColorIndex.TryGet(orb, out damageColorIndex);
        }

        public static bool TryGetDamageType(this Orb orb, out DamageTypeCombo damageType)
        {
            if (orb is null)
            {
                damageType = default;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.DamageType.TryGet(orb, out damageType);
        }

        public static bool TryGetProcCoefficient(this Orb orb, out float procCoefficient)
        {
            if (orb is null)
            {
                procCoefficient = default;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            return orbFields.ProcCoefficient.TryGet(orb, out procCoefficient);
        }
    }
}
