using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    public class ChaosEffectNameFormattersNetworker : NetworkBehaviour
    {
        static ChaosEffectNameFormattersNetworker _instance;
        public static ChaosEffectNameFormattersNetworker Instance => _instance;

        EffectNameFormatter[] _effectNameFormatters;
        const uint NAME_FORMATTERS_DIRTY_BIT = 1 << 0;

        void Awake()
        {
            _effectNameFormatters = new EffectNameFormatter[ChaosEffectCatalog.EffectCount];
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            if (NetworkServer.active)
            {
                ChaosEffectInfo.OnEffectNameFormatterDirty += onEffectNameFormatterDirtyServer;
            }
        }

        void OnDisable()
        {
            ChaosEffectInfo.OnEffectNameFormatterDirty -= onEffectNameFormatterDirtyServer;

            SingletonHelper.Unassign(ref _instance, this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            refreshNameFormatters();
        }

        void onEffectNameFormatterDirtyServer(ChaosEffectInfo effectInfo)
        {
            refreshNameFormatters();
        }

        [Server]
        void refreshNameFormatters()
        {
            bool anyNameFormatterChanged = false;
            for (ChaosEffectIndex effectIndex = 0; (int)effectIndex < _effectNameFormatters.Length; effectIndex++)
            {
                ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(effectIndex);

                EffectNameFormatter newNameFormatter = null;
                if (effectInfo != null)
                {
                    newNameFormatter = effectInfo.LocalDisplayNameFormatter;
                }

                if (newNameFormatter is EffectNameFormatter_None)
                    newNameFormatter = null;

                ref EffectNameFormatter currentNameFormatter = ref _effectNameFormatters[(int)effectIndex];

                bool currentIsNull = currentNameFormatter is null;
                bool newIsNull = newNameFormatter is null;

                if (currentIsNull != newIsNull || (!currentIsNull && !newIsNull && !currentNameFormatter.Equals(newNameFormatter)))
                {
                    currentNameFormatter = newNameFormatter;
                    anyNameFormatterChanged = true;
                }
            }

            if (anyNameFormatterChanged)
            {
                SetDirtyBit(NAME_FORMATTERS_DIRTY_BIT);
            }
        }

        public EffectNameFormatter GetNameFormatter(ChaosEffectIndex chaosEffectIndex)
        {
            if (chaosEffectIndex < 0 || (int)chaosEffectIndex >= _effectNameFormatters.Length)
                return EffectNameFormatter_None.Instance;

            return _effectNameFormatters[(int)chaosEffectIndex] ?? EffectNameFormatter_None.Instance;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            uint dirtyBits;
            if (initialState)
            {
                dirtyBits = ~0u;
            }
            else
            {
                dirtyBits = syncVarDirtyBits;
                writer.WritePackedUInt32(dirtyBits);
            }

            bool anythingWritten = false;

            if ((dirtyBits & NAME_FORMATTERS_DIRTY_BIT) != 0)
            {
                List<ChaosEffectIndex> validNameFormatterIndices = new List<ChaosEffectIndex>(_effectNameFormatters.Length);
                for (ChaosEffectIndex effectIndex = 0; (int)effectIndex < _effectNameFormatters.Length; effectIndex++)
                {
                    if (_effectNameFormatters[(int)effectIndex] is not null)
                    {
                        validNameFormatterIndices.Add(effectIndex);
                    }
                }

                writer.WritePackedUInt32((uint)validNameFormatterIndices.Count);

                foreach (ChaosEffectIndex effectIndex in validNameFormatterIndices)
                {
                    writer.WriteChaosEffectIndex(effectIndex);
                    writer.Write(_effectNameFormatters[(int)effectIndex]);
                }

                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            uint dirtyBits = initialState ? ~0u : reader.ReadPackedUInt32();

            if ((dirtyBits & NAME_FORMATTERS_DIRTY_BIT) != 0)
            {
                Array.Clear(_effectNameFormatters, 0, _effectNameFormatters.Length);

                uint nameFormattersCount = reader.ReadPackedUInt32();
                for (int i = 0; i < nameFormattersCount; i++)
                {
                    ChaosEffectIndex chaosEffectIndex = reader.ReadChaosEffectIndex();
                    EffectNameFormatter effectNameFormatter = reader.ReadEffectNameFormatter();

                    if (chaosEffectIndex >= 0 && (int)chaosEffectIndex < _effectNameFormatters.Length)
                    {
                        _effectNameFormatters[(int)chaosEffectIndex] = effectNameFormatter;
                    }
                    else
                    {
                        Log.Error($"Effect index out of range! i={i}, effectIndex={chaosEffectIndex}, nameFormatter={effectNameFormatter}");
                    }
                }
            }
        }
    }
}
