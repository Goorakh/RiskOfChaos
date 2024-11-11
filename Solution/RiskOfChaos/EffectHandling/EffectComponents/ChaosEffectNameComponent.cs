using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.Formatting;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    [RequiredComponents(typeof(ChaosEffectComponent))]
    public sealed class ChaosEffectNameComponent : NetworkBehaviour
    {
        EffectNameFormatterProvider _staticNameFormatterProvider;

        const uint CUSTOM_NAME_FORMATTER_DIRTY_BIT = 1 << 0;

        bool _hasCustomNameFormatter;

        public EffectNameFormatterProvider NameFormatterProvider { get; private set; }

        void Awake()
        {
            ChaosEffectComponent effectComponent = GetComponent<ChaosEffectComponent>();

            if (effectComponent && effectComponent.ChaosEffectInfo != null)
            {
                _staticNameFormatterProvider = effectComponent.ChaosEffectInfo.StaticDisplayNameFormatterProvider;
            }

            EffectNameFormatter nameFormatter = EffectNameFormatter_None.Instance;
            if (_staticNameFormatterProvider != null)
            {
                nameFormatter = _staticNameFormatterProvider.NameFormatter;
                _staticNameFormatterProvider.OnNameFormatterChanged += onStaticNameFormatterChanged;
            }

            NameFormatterProvider = new EffectNameFormatterProvider(nameFormatter, false);
            NameFormatterProvider.OnNameFormatterChanged += onNameFormatterChanged;
        }

        void OnDestroy()
        {
            if (NameFormatterProvider != null)
            {
                NameFormatterProvider.OnNameFormatterChanged -= onNameFormatterChanged;
                NameFormatterProvider.Dispose();
            }

            if (_staticNameFormatterProvider != null)
            {
                _staticNameFormatterProvider.OnNameFormatterChanged -= onStaticNameFormatterChanged;
            }
        }

        void onNameFormatterChanged()
        {
            if (NetworkServer.active)
            {
                if (_hasCustomNameFormatter)
                {
                    SetDirtyBit(CUSTOM_NAME_FORMATTER_DIRTY_BIT);
                }
            }
        }

        void onStaticNameFormatterChanged()
        {
            refreshCurrentNameFormatter();
        }

        void refreshCurrentNameFormatter()
        {
            if (!_hasCustomNameFormatter)
            {
                NameFormatterProvider.NameFormatter = _staticNameFormatterProvider.NameFormatter;
            }

            NameFormatterProvider.HasNameFormatterOwnership = _hasCustomNameFormatter;
        }

        [Server]
        public void SetCustomNameFormatter(EffectNameFormatter nameFormatter)
        {
            bool customNameFormatterDirty = false;

            if (!_hasCustomNameFormatter)
            {
                customNameFormatterDirty = true;
            }

            _hasCustomNameFormatter = true;
            if (NameFormatterProvider.NameFormatter != nameFormatter)
            {
                customNameFormatterDirty = true;
                NameFormatterProvider.NameFormatter = nameFormatter;
            }

            refreshCurrentNameFormatter();

            if (customNameFormatterDirty)
            {
                SetDirtyBit(CUSTOM_NAME_FORMATTER_DIRTY_BIT);
            }
        }

        [Server]
        public void RemoveCustomNameFormatter()
        {
            if (!_hasCustomNameFormatter)
                return;

            _hasCustomNameFormatter = false;
            refreshCurrentNameFormatter();
            SetDirtyBit(CUSTOM_NAME_FORMATTER_DIRTY_BIT);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            uint dirtyBits = initialState ? ~0u : syncVarDirtyBits;
            if (!initialState)
            {
                writer.WritePackedUInt32(dirtyBits);
            }

            bool anythingWritten = false;

            if ((dirtyBits & CUSTOM_NAME_FORMATTER_DIRTY_BIT) != 0)
            {
                anythingWritten = true;

                writer.Write(_hasCustomNameFormatter);
                if (_hasCustomNameFormatter)
                {
                    writer.Write(NameFormatterProvider.NameFormatter);
                }
            }

            return anythingWritten || initialState;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            uint dirtyBits = initialState ? ~0u : reader.ReadPackedUInt32();

            if ((dirtyBits & CUSTOM_NAME_FORMATTER_DIRTY_BIT) != 0)
            {
                _hasCustomNameFormatter = reader.ReadBoolean();

                if (_hasCustomNameFormatter)
                {
                    NameFormatterProvider.NameFormatter = reader.ReadEffectNameFormatter();
                }

                refreshCurrentNameFormatter();
            }
        }
    }
}
