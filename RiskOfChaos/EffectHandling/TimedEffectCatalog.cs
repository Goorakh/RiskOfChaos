using HG;
using RiskOfChaos.EffectDefinitions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RiskOfChaos.EffectHandling
{
    public static class TimedEffectCatalog
    {
        static TimedEffectInfo[] _timedEffectInfos;

        static readonly Dictionary<int, int> _effectIndexToTimedEffectIndexMap = new Dictionary<int, int>();

        static int _timedEffectCount;
        public static int TimedEffectCount => _timedEffectCount;

        public static readonly ResourceAvailability Availability = new ResourceAvailability();

        [SystemInitializer]
        static void Init()
        {
            ChaosEffectCatalog.Availability.CallWhenAvailable(() =>
            {
                _timedEffectInfos = ChaosEffectCatalog.AllEffects()
                                                      .Where(e => typeof(TimedEffect).IsAssignableFrom(e.EffectType))
                                                      .Select((effectInfo, i) => new TimedEffectInfo(effectInfo, i))
                                                      .ToArray();

                _timedEffectCount = _timedEffectInfos.Length;

                for (int i = 0; i < _timedEffectCount; i++)
                {
                    _timedEffectInfos[i].AddRiskOfOptionsEntries();

                    _effectIndexToTimedEffectIndexMap.Add(_timedEffectInfos[i].EffectIndex, _timedEffectInfos[i].TimedEffectIndex);
                }

                ChaosEffectCatalog.EffectDisplayNameModificationProvider += addTimedTypeToEffectName;
                ChaosEffectCatalog.OnEffectInstantiatedServer += onEffectInstantiatedServer;

                Availability.MakeAvailable();
            });
        }

        static void addTimedTypeToEffectName(in ChaosEffectInfo effectInfo, ref string displayName)
        {
            if (!TryFindTimedEffectInfo(effectInfo, out TimedEffectInfo timedEffectInfo))
                return;

            displayName = timedEffectInfo.ApplyTimedTypeSuffix(displayName);
        }

        static void onEffectInstantiatedServer(in ChaosEffectInfo effectInfo, in CreateEffectInstanceArgs args, BaseEffect instance)
        {
            if (!TryFindTimedEffectInfo(effectInfo, out TimedEffectInfo timedEffectInfo))
                return;

            if (instance is TimedEffect timedEffect)
            {
                timedEffect.TimedType = timedEffectInfo.TimedType;
                timedEffect.DurationSeconds = timedEffectInfo.DurationSeconds;
            }
            else
            {
                Log.Error($"Effect info {effectInfo} is marked as timed, but instance is not of type {nameof(TimedEffect)} ({instance})");
            }
        }

        public static TimedEffectInfo GetTimedEffectInfo(int timedEffectIndex)
        {
            return ArrayUtils.GetSafe(_timedEffectInfos, timedEffectIndex);
        }

        public static bool TryFindTimedEffectInfo(in ChaosEffectInfo effectInfo, out TimedEffectInfo timedEffectInfo)
        {
            if (_effectIndexToTimedEffectIndexMap.TryGetValue(effectInfo.EffectIndex, out int timedEffectIndex))
            {
                timedEffectInfo = GetTimedEffectInfo(timedEffectIndex);
                return true;
            }
            else
            {
                timedEffectInfo = default;
                return false;
            }
        }
    }
}
