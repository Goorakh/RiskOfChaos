using BepInEx.Configuration;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ChaosEffectAttribute : HG.Reflection.SearchableAttribute
    {
        public readonly string Identifier;

        public string ConfigName { get; set; } = null;

        public float DefaultSelectionWeight { get; set; } = 1f;

        public new Type target => base.target as Type;

        public ChaosEffectAttribute(string identifier)
        {
            Identifier = identifier;
        }

        internal virtual bool Validate()
        {
            if (target == null)
            {
                Log.Warning($"Invalid attribute target ({base.target})");
                return false;
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(target))
            {
                Log.Error($"Effect '{Identifier}' type ({target.FullName}) does not derive from {nameof(MonoBehaviour)}");
                return false;
            }

            if (DefaultSelectionWeight < 0f)
            {
                Log.Error($"Effect '{Identifier}' has invalid default weight: {DefaultSelectionWeight} (Must be >= 0)");
                return false;
            }

            return true;
        }

        public virtual ChaosEffectInfo BuildEffectInfo(ChaosEffectIndex index, ConfigFile configFile)
        {
            return new ChaosEffectInfo(index, this, configFile);
        }
    }
}
