using HG;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    public sealed class ChaosEffectNameFormattersNetworker : NetworkBehaviour
    {
        static ChaosEffectNameFormattersNetworker _instance;
        public static ChaosEffectNameFormattersNetworker Instance => _instance;

        const uint NAME_FORMATTERS_DIRTY_BIT = 1 << 0;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            foreach (ChaosEffectInfo effectInfo in ChaosEffectCatalog.AllEffects)
            {
                effectInfo.StaticDisplayNameFormatterProvider.OnNameFormatterChanged += onEffectNameFormatterChanged;
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            foreach (ChaosEffectInfo effectInfo in ChaosEffectCatalog.AllEffects)
            {
                effectInfo.StaticDisplayNameFormatterProvider.OnNameFormatterChanged -= onEffectNameFormatterChanged;
                effectInfo.RestoreStaticDisplayNameFormatter();
            }
        }

        void onEffectNameFormatterChanged()
        {
            if (NetworkServer.active)
            {
                SetDirtyBit(NAME_FORMATTERS_DIRTY_BIT);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            uint dirtyBits = initialState ? ~0u : syncVarDirtyBits;
            if (!initialState)
            {
                writer.WritePackedUInt32(dirtyBits);
            }

            bool anythingWritten = false;

            if ((dirtyBits & NAME_FORMATTERS_DIRTY_BIT) != 0)
            {
                List<ChaosEffectInfo> validNameFormatterIndices = new List<ChaosEffectInfo>(ChaosEffectCatalog.EffectCount);
                foreach (ChaosEffectInfo effectInfo in ChaosEffectCatalog.AllEffects)
                {
                    EffectNameFormatter nameFormatter = effectInfo.StaticDisplayNameFormatterProvider.NameFormatter;
                    if (nameFormatter != null && nameFormatter is not EffectNameFormatter_None)
                    {
                        validNameFormatterIndices.Add(effectInfo);
                    }
                }

                writer.WritePackedUInt32((uint)validNameFormatterIndices.Count);

                foreach (ChaosEffectInfo effectInfo in validNameFormatterIndices)
                {
                    ChaosEffectIndex effectIndex = ChaosEffectIndex.Invalid;
                    EffectNameFormatter nameFormatter = EffectNameFormatter_None.Instance;
                    if (effectInfo != null)
                    {
                        effectIndex = effectInfo.EffectIndex;
                        nameFormatter = effectInfo.StaticDisplayNameFormatterProvider.NameFormatter;
                    }

                    writer.WriteChaosEffectIndex(effectIndex);
                    writer.Write(nameFormatter);
                }

                anythingWritten = true;
            }

            return anythingWritten || initialState;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            uint dirtyBits = initialState ? ~0u : reader.ReadPackedUInt32();

            if ((dirtyBits & NAME_FORMATTERS_DIRTY_BIT) != 0)
            {
                EffectNameFormatter[] nameFormatters = new EffectNameFormatter[ChaosEffectCatalog.EffectCount];

                for (int i = 0; i < ChaosEffectCatalog.EffectCount; i++)
                {
                    ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo((ChaosEffectIndex)i);

                    nameFormatters[i] = effectInfo?.GetDefaultNameFormatter() ?? EffectNameFormatter_None.Instance;
                }

                uint nameFormattersCount = reader.ReadPackedUInt32();
                for (int i = 0; i < nameFormattersCount; i++)
                {
                    ChaosEffectIndex effectIndex = reader.ReadChaosEffectIndex();
                    EffectNameFormatter nameFormatter = reader.ReadEffectNameFormatter();

                    if (ArrayUtils.IsInBounds(nameFormatters, (int)effectIndex))
                    {
                        nameFormatters[(int)effectIndex] = nameFormatter;
                    }
                    else
                    {
                        Log.Error($"Effect index out of range! i={i}, effectIndex={effectIndex}, nameFormatter={nameFormatter}");
                    }
                }

                for (int i = 0; i < ChaosEffectCatalog.EffectCount; i++)
                {
                    ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo((ChaosEffectIndex)i);
                    if (effectInfo != null)
                    {
                        effectInfo.StaticDisplayNameFormatterProvider.NameFormatter = nameFormatters[i];
                    }
                }
            }
        }
    }
}
