using HG;
using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Cost
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class CostConversionProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        CostTypeIndex[] _conversionsLookup;
        const uint CONVERSIONS_LOOKUP_DIRTY_BIT = 1 << 0;

        void Awake()
        {
            _conversionsLookup = new CostTypeIndex[CostTypeCatalog.costTypeCount];
            _modificationController = GetComponent<ValueModificationController>();
        }

        public void SetCostTypeConversion(CostTypeIndex from, CostTypeIndex? to)
        {
            if (!ArrayUtils.IsInBounds(_conversionsLookup, (int)from))
            {
                Log.Error($"'{nameof(from)}' cost type out of range: {from}");
                return;
            }

            ref CostTypeIndex costConversionValue = ref _conversionsLookup[(int)from];
            CostTypeIndex newCostConversionValue = to.HasValue ? to.Value + 1 : 0;

            if (newCostConversionValue != costConversionValue)
            {
                costConversionValue = newCostConversionValue;
                SetDirtyBit(CONVERSIONS_LOOKUP_DIRTY_BIT);
                _modificationController.InvokeOnValuesDirty();
            }
        }

        public bool TryConvertCostType(ref CostModificationInfo costModification, OriginalCostProvider originalCost)
        {
            CostTypeIndex convertToCostType = ArrayUtils.GetSafe(_conversionsLookup, (int)costModification.CostType) - 1;
            if (convertToCostType < 0 || convertToCostType == costModification.CostType)
                return false;

            costModification.CurrentCost = (float)CostUtils.ConvertCost(originalCost.BaseCost, costModification.CostType, convertToCostType);
            costModification.CostType = convertToCostType;

            return true;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            uint dirtyBits = initialState ? ~0U : syncVarDirtyBits;
            if (!initialState)
            {
                writer.WritePackedUInt32(dirtyBits);
            }

            bool anythingWritten = false;

            if ((dirtyBits & CONVERSIONS_LOOKUP_DIRTY_BIT) != 0)
            {
                anythingWritten = true;

                foreach (CostTypeIndex conversionCostType in _conversionsLookup)
                {
                    writer.WritePackedUInt32((uint)conversionCostType);
                }
            }

            return initialState || anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            uint dirtyBits = initialState ? ~0U : reader.ReadPackedUInt32();

            if ((dirtyBits & CONVERSIONS_LOOKUP_DIRTY_BIT) != 0)
            {
                for (int i = 0; i < _conversionsLookup.Length; i++)
                {
                    _conversionsLookup[i] = (CostTypeIndex)reader.ReadPackedUInt32();
                }

                _modificationController.InvokeOnValuesDirty();
            }
        }
    }
}
