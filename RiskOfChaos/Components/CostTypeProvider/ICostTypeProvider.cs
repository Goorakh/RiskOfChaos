using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components.CostTypeProvider
{
    public interface ICostTypeProvider
    {
        CostTypeIndex CostType { get; set; }
    }
}
