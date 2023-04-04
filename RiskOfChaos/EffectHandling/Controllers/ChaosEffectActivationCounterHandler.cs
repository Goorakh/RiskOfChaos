using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(true)]
    public class ChaosEffectActivationCounterHandler : MonoBehaviour
    {
        ChaosEffectActivationCounter[] _effectActivationCounts = Array.Empty<ChaosEffectActivationCounter>();

        static ChaosEffectActivationCounterHandler _instance;
        public static ChaosEffectActivationCounterHandler Instance => _instance;

        void Awake()
        {
            ChaosEffectCatalog.Availability.CallWhenAvailable(() =>
            {
#if DEBUG
                Log.Debug("Initialized effect activation counter array");
#endif

                _effectActivationCounts = ChaosEffectCatalog.PerEffectArray<ChaosEffectActivationCounter>();
                for (int i = 0; i < ChaosEffectCatalog.EffectCount; i++)
                {
                    _effectActivationCounts[i] = new ChaosEffectActivationCounter(i);
                }
            });
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            Stage.onServerStageComplete += Stage_onServerStageComplete;

            resetAllCounters();
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            Stage.onServerStageComplete -= Stage_onServerStageComplete;

            resetAllCounters();
        }

        void resetAllCounters()
        {
            for (int i = 0; i < _effectActivationCounts.Length; i++)
            {
                ref ChaosEffectActivationCounter activationCounter = ref _effectActivationCounts[i];
                activationCounter.StageActivations = 0;
                activationCounter.RunActivations = 0;
            }

#if DEBUG
            Log.Debug("Reset all effect activation counters");
#endif
        }

        void Stage_onServerStageComplete(Stage _)
        {
            resetStageCounters();
        }

        void resetStageCounters()
        {
            for (int i = 0; i < _effectActivationCounts.Length; i++)
            {
                ref ChaosEffectActivationCounter activationCounter = ref _effectActivationCounts[i];
                activationCounter.StageActivations = 0;
            }

#if DEBUG
            Log.Debug("Reset effect stage activation counters");
#endif
        }

        public void IncrementEffectActivationCounter(in ChaosEffectInfo effectInfo)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            try
            {
                ref ChaosEffectActivationCounter activationCounter = ref getEffectActivationCounterUncheckedRef(effectInfo);
                activationCounter.StageActivations++;
                activationCounter.RunActivations++;

#if DEBUG
                Log.Debug($"increased effect activation counter: {activationCounter}");
#endif
            }
            catch (IndexOutOfRangeException ex)
            {
                Log.Error($"{nameof(IndexOutOfRangeException)} in {nameof(getEffectActivationCounterUncheckedRef)}, invalid effect index? {nameof(effectInfo)}={effectInfo}: {ex}");
            }
        }

        ref ChaosEffectActivationCounter getEffectActivationCounterUncheckedRef(in ChaosEffectInfo effectInfo)
        {
            return ref _effectActivationCounts[effectInfo.EffectIndex];
        }

        ChaosEffectActivationCounter getEffectActivationCounter(in ChaosEffectInfo effectInfo)
        {
            if (effectInfo.EffectIndex < 0 || effectInfo.EffectIndex >= _effectActivationCounts.Length)
                return ChaosEffectActivationCounter.EmptyCounter;

            return getEffectActivationCounterUncheckedRef(effectInfo);
        }

        public int GetTotalRunEffectActivationCount(in ChaosEffectInfo effectInfo)
        {
            return getEffectActivationCounter(effectInfo).RunActivations;
        }

        public int GetTotalStageEffectActivationCount(in ChaosEffectInfo effectInfo)
        {
            return getEffectActivationCounter(effectInfo).StageActivations;
        }

        public int GetEffectActivationCount(in ChaosEffectInfo effectInfo, EffectActivationCountMode mode)
        {
            return mode switch
            {
                EffectActivationCountMode.PerStage => GetTotalStageEffectActivationCount(effectInfo),
                EffectActivationCountMode.PerRun => GetTotalRunEffectActivationCount(effectInfo),
                _ => 0,
            };
        }
    }
}
