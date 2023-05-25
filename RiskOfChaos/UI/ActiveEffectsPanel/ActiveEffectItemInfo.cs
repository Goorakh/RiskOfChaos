using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
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

        public ActiveEffectItemInfo(TimedEffectInfo effectInfo, TimedEffect effectInstance)
        {
            EffectInfo = effectInfo;
            DispatchID = effectInstance.DispatchID;

            DisplayName = ChaosEffectCatalog.GetEffectInfo(effectInfo.EffectIndex).DisplayName;

            TimedType = effectInstance.TimedType;
            DurationSeconds = effectInstance.DurationSeconds;

            if (Run.instance)
            {
                TimeStarted = Run.instance.GetRunTime(RunTimerType.Realtime);
            }
        }

        private ActiveEffectItemInfo(NetworkReader reader)
        {
            if (!reader.ReadBoolean())
                return;

            EffectInfo = TimedEffectCatalog.GetTimedEffectInfo(reader.ReadTimedChaosEffectIndex());
            DispatchID = reader.ReadPackedUInt64();

            DisplayName = reader.ReadString();

            TimedType = (TimedEffectType)reader.ReadByte();
            if (TimedType == TimedEffectType.FixedDuration)
            {
                DurationSeconds = reader.ReadSingle();
            }

            TimeStarted = reader.ReadSingle();
        }

        public void Serialize(NetworkWriter writer)
        {
            // For some stupid reason SyncList calls serialize with default(T), when RemoveAt is used
            if (EffectInfo == null)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            writer.WriteTimedChaosEffectIndex(EffectInfo.TimedEffectIndex);
            writer.WritePackedUInt64(DispatchID);

            writer.Write(DisplayName);

            writer.Write((byte)TimedType);
            if (TimedType == TimedEffectType.FixedDuration)
            {
                writer.Write(DurationSeconds);
            }

            writer.Write(TimeStarted);
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
            return DispatchID == other.DispatchID;
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
