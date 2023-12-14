using RoR2;
using RoR2.Orbs;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class OrbExtensions
    {
        public static CharacterBody GetAttacker(this Orb orb)
        {
            if (orb is null)
                return null;

            FieldInfo attackerField = orb.GetType().GetField("attacker");
            if (attackerField is null)
                return null;

            if (attackerField.FieldType == typeof(CharacterBody))
            {
                return (CharacterBody)attackerField.GetValue(orb);
            }
            else if (attackerField.FieldType == typeof(GameObject))
            {
                GameObject attacker = (GameObject)attackerField.GetValue(orb);
                if (attacker)
                {
                    return attacker.GetComponent<CharacterBody>();
                }
            }

            return null;
        }
    }
}
