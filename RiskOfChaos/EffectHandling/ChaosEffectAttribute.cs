using System;

namespace RiskOfChaos.EffectHandling
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ChaosEffectAttribute : HG.Reflection.SearchableAttribute
    {
        public readonly string Identifier;

        public string ConfigName { get; set; } = null;

        // public bool HasDescription { get; set; }

        public float DefaultSelectionWeight { get; set; } = 1f;

        public float EffectWeightReductionPercentagePerActivation { get; set; } = 5f;

        public EffectActivationCountMode EffectRepetitionWeightCalculationMode { get; set; } = EffectActivationCountMode.PerStage;

        public int EffectActivationCountHardCap { get; set; } = -1;

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
