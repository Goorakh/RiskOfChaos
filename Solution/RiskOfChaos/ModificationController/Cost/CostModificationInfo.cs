using RiskOfChaos.Components.CostProviders;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.ModificationController.Cost
{
    public struct CostModificationInfo : ICostProvider
    {
        readonly ICostProvider _costProvider;

        public CostTypeIndex CostType;
        public float CostMultiplier;

        public float CurrentCost
        {
            readonly get => _costProvider.Cost * CostMultiplier;
            set => CostMultiplier = Mathf.Max(0f, value / _costProvider.Cost);
        }

        int ICostProvider.Cost
        {
            readonly get => Mathf.RoundToInt(CurrentCost);
            set => CurrentCost = Mathf.Max(0, value);
        }

        CostTypeIndex ICostProvider.CostType
        {
            readonly get => CostType;
            set => CostType = value;
        }

        public CostModificationInfo(ICostProvider costProvider)
        {
            _costProvider = costProvider;

            CostType = _costProvider.CostType;
            CostMultiplier = 1f;
        }
    }
}
