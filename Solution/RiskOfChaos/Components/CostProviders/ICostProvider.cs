using RoR2;

namespace RiskOfChaos.Components.CostProviders
{
    public interface ICostProvider
    {
        CostTypeIndex CostType { get; set; }

        int Cost { get; set; }
    }
}
