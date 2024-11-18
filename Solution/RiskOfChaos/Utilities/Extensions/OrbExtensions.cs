using RiskOfChaos.Collections;
using RiskOfChaos.Utilities.Reflection;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
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
            if (!orbFields.Attacker.IsValid)
                return null;

            try
            {
                return orbFields.Attacker.Get(orb);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);
                return null;
            }
        }

        public static bool TryGetProcChainMask(this Orb orb, out ProcChainMask procChainMask)
        {
            if (orb is null)
            {
                procChainMask = default;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            if (!orbFields.ProcChainMask.IsValid)
            {
                procChainMask = default;
                return false;
            }

            try
            {
                procChainMask = orbFields.ProcChainMask.Get(orb);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);

                procChainMask = default;
                return false;
            }

            return true;
        }

        public static bool TrySetProcChainMask(this Orb orb, ProcChainMask procChainMask)
        {
            if (orb is null)
                return false;

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            if (!orbFields.ProcChainMask.IsValid)
                return false;

            try
            {
                orbFields.ProcChainMask.Set(orb, procChainMask);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);

                return false;
            }

            return true;
        }

        public static bool TryGetTeamIndex(this Orb orb, out TeamIndex teamIndex)
        {
            if (orb is null)
            {
                teamIndex = TeamIndex.None;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            FieldWrapper<TeamIndex, Orb> teamIndexField = orbFields.TeamIndex;

            if (!teamIndexField.IsValid)
            {
                teamIndex = TeamIndex.None;
                return false;
            }

            try
            {
                teamIndex = teamIndexField.Get(orb);
                return true;
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);

                teamIndex = TeamIndex.None;
                return false;
            }
        }

        public static bool TryGetBouncedObjects(this Orb orb, out ReadOnlyCollection<HealthComponent> bouncedObjects)
        {
            if (orb is null)
            {
                bouncedObjects = Empty<HealthComponent>.ReadOnlyCollection;
                return false;
            }

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            FieldWrapper<List<HealthComponent>, Orb> bouncedObjectsField = orbFields.BouncedObjects;
            if (!bouncedObjectsField.IsValid)
            {
                bouncedObjects = Empty<HealthComponent>.ReadOnlyCollection;
                return false;
            }

            try
            {
                bouncedObjects = bouncedObjectsField.Get(orb)?.AsReadOnly() ?? Empty<HealthComponent>.ReadOnlyCollection;
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);
                bouncedObjects = Empty<HealthComponent>.ReadOnlyCollection;
                return false;
            }

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
            FieldWrapper<int, Orb> bouncesRemainingField = orbFields.BouncesRemaining;
            if (!bouncesRemainingField.IsValid)
            {
                bouncesRemaining = 0;
                return false;
            }

            try
            {
                bouncesRemaining = bouncesRemainingField.Get(orb);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);

                bouncesRemaining = 0;
                return false;
            }

            return true;
        }

        public static bool TrySetBouncesRemaining(this Orb orb, int bouncesRemaining)
        {
            if (orb is null)
                return false;

            CachedOrbFields orbFields = getOrCreateOrbFields(orb.GetType());
            FieldWrapper<int, Orb> bouncesRemainingField = orbFields.BouncesRemaining;

            if (!bouncesRemainingField.IsValid)
                return false;

            try
            {
                bouncesRemainingField.Set(orb, bouncesRemaining);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix(e);

                return false;
            }

            return true;
        }
    }
}
