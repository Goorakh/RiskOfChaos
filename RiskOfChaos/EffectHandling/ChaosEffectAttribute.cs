using System;

namespace RiskOfChaos.EffectHandling
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ChaosEffectAttribute : HG.Reflection.SearchableAttribute
    {
        public readonly string Identifier;

        // public bool HasDescription { get; set; }

        public float DefaultSelectionWeight { get; set; } = 1f;

        public ChaosEffectAttribute(string identifier)
        {
            Identifier = identifier;
        }

        internal ChaosEffectInfo BuildEffectInfo(int index)
        {
            return new ChaosEffectInfo(index, this);
        }
    }
}
