using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Extensions;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public readonly struct ActiveEffectItemInfo : IEquatable<ActiveEffectItemInfo>
    {
        public readonly TimedEffectInfo EffectInfo;
        public readonly ulong DispatchID;

        public readonly string DisplayName;
        public readonly TimedEffectType TimedType;
        public readonly float DurationSeconds;
        public readonly float TimeStarted;

        public readonly bool ShouldDisplay;

        public readonly uint Version;

        public ActiveEffectItemInfo(TimedEffect effectInstance, uint version)
        {
            EffectInfo = effectInstance.EffectInfo;
            DispatchID = effectInstance.DispatchID;

            DisplayName = EffectInfo.GetDisplayName(EffectNameFormatFlags.RuntimeFormatArgs);

            TimedType = effectInstance.TimedType;
            DurationSeconds = effectInstance.DurationSeconds;

            TimeStarted = effectInstance.TimeStarted;

            ShouldDisplay = EffectInfo.ShouldDisplayOnHUD;

            Version = version;
        }

        private ActiveEffectItemInfo(NetworkReader reader)
        {
            if (!reader.ReadBoolean())
                return;

            ChaosEffectIndex effectIndex = reader.ReadChaosEffectIndex();

            if (ChaosEffectCatalog.GetEffectInfo(effectIndex) is not TimedEffectInfo effectInfo)
            {
                Log.Error($"Effect index {effectIndex} is not a TimedEffectInfo");
                return;
            }

            EffectInfo = effectInfo;

            DispatchID = reader.ReadPackedUInt64();

            string displayName = reader.ReadString();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = EffectInfo.GetDisplayName(EffectNameFormatFlags.RuntimeFormatArgs);
            }

            DisplayName = displayName;

            TimedType = (TimedEffectType)reader.ReadByte();
            if (TimedType == TimedEffectType.FixedDuration)
            {
                DurationSeconds = reader.ReadSingle();
            }

            TimeStarted = reader.ReadSingle();

            ShouldDisplay = reader.ReadBoolean();

            Version = reader.ReadPackedUInt32();
        }

        public void Serialize(NetworkWriter writer)
        {
            // For some stupid reason SyncList calls serialize with default(T) when RemoveAt is used
            if (EffectInfo == null)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            writer.WriteChaosEffectIndex(EffectInfo.EffectIndex);
            writer.WritePackedUInt64(DispatchID);

            // If the effect name has custom runtime formatting, send the display name to clients. Otherwise, let the clients look up the effect name in their ChaosEffectCatalog instead to save on message size
            writer.Write(EffectInfo.HasCustomDisplayNameFormatter ? DisplayName : string.Empty);

            writer.Write((byte)TimedType);
            if (TimedType == TimedEffectType.FixedDuration)
            {
                writer.Write(DurationSeconds);
            }

            writer.Write(TimeStarted);

            writer.Write(ShouldDisplay);

            writer.WritePackedUInt32(Version);
        }

        public static ActiveEffectItemInfo Deserialize(NetworkReader reader)
        {
            return new ActiveEffectItemInfo(reader);
        }

        public override bool Equals(object obj)
        {
            return obj is ActiveEffectItemInfo info && Equals(info);
        }

        public bool Equals(ActiveEffectItemInfo other)
        {
            return EffectInfo == other.EffectInfo && DispatchID == other.DispatchID && Version == other.Version;
        }

        public override int GetHashCode()
        {
            return DispatchID.GetHashCode();
        }

        public static bool operator ==(ActiveEffectItemInfo left, ActiveEffectItemInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActiveEffectItemInfo left, ActiveEffectItemInfo right)
        {
            return !(left == right);
        }
    }
}
