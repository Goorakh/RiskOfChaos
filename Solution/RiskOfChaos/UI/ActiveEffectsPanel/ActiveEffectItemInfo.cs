using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities.Extensions;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.UI.ActiveEffectsPanel
{
    public readonly struct ActiveEffectItemInfo : IEquatable<ActiveEffectItemInfo>
    {
        public readonly TimedEffectInfo EffectInfo;
        public readonly ulong DispatchID;

        public readonly EffectNameFormatter NameFormatter;

        public readonly TimedEffectType TimedType;
        public readonly float DurationSeconds;
        public readonly float TimeStarted;

        public readonly float RemainingStocks;

        public readonly bool ShouldDisplay;

        public readonly string DisplayName => EffectInfo?.GetDisplayName(NameFormatter, EffectNameFormatFlags.RuntimeFormatArgs) ?? "NULL";

        public readonly float EndTime => TimeStarted + (DurationSeconds * RemainingStocks);

        public ActiveEffectItemInfo(TimedEffect effectInstance)
        {
            EffectInfo = effectInstance.EffectInfo;
            DispatchID = effectInstance.DispatchID;

            NameFormatter = EffectInfo.LocalDisplayNameFormatter;

            TimedType = effectInstance.TimedType;
            DurationSeconds = effectInstance.DurationSeconds;

            TimeStarted = effectInstance.TimeStarted;

            RemainingStocks = effectInstance.StocksRemaining;

            ShouldDisplay = effectInstance.ShouldDisplayOnHUD;
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

            NameFormatter = reader.ReadEffectNameFormatter();

            TimedType = (TimedEffectType)reader.ReadByte();
            if (TimedType == TimedEffectType.FixedDuration)
            {
                DurationSeconds = reader.ReadSingle();
            }

            TimeStarted = reader.ReadSingle();

            RemainingStocks = reader.ReadSingle();

            ShouldDisplay = reader.ReadBoolean();
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

            writer.Write(NameFormatter);

            writer.Write((byte)TimedType);
            if (TimedType == TimedEffectType.FixedDuration)
            {
                writer.Write(DurationSeconds);
            }

            writer.Write(TimeStarted);

            writer.Write(RemainingStocks);

            writer.Write(ShouldDisplay);
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
            return EffectInfo == other.EffectInfo &&
                   DispatchID == other.DispatchID &&
                   NameFormatter == other.NameFormatter &&
                   TimedType == other.TimedType &&
                   DurationSeconds == other.DurationSeconds &&
                   TimeStarted == other.TimeStarted &&
                   RemainingStocks == other.RemainingStocks &&
                   ShouldDisplay == other.ShouldDisplay;
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
