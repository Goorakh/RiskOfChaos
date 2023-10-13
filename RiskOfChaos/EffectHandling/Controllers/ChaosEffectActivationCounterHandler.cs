using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using RoR2;
using System;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(true)]
    public class ChaosEffectActivationCounterHandler : MonoBehaviour
    {
        ChaosEffectActivationCounter[] _effectActivationCounts = Array.Empty<ChaosEffectActivationCounter>();

        static ChaosEffectActivationCounterHandler _instance;
        public static ChaosEffectActivationCounterHandler Instance => _instance;

        ChaosEffectDispatcher _effectDispatcher;

        void Awake()
        {
            _effectDispatcher = GetComponent<ChaosEffectDispatcher>();

            ChaosEffectCatalog.Availability.CallWhenAvailable(() =>
            {
#if DEBUG
                Log.Debug("Initialized effect activation counter array");
#endif

                _effectActivationCounts = ChaosEffectCatalog.PerEffectArray<ChaosEffectActivationCounter>();
                for (int i = 0; i < ChaosEffectCatalog.EffectCount; i++)
                {
                    _effectActivationCounts[i] = new ChaosEffectActivationCounter((ChaosEffectIndex)i);
                }
            });
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            Stage.onServerStageComplete += Stage_onServerStageComplete;

            _effectDispatcher.OnEffectAboutToDispatchServer += onEffectAboutToDispatchServer;

            resetAllCounters();

            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                SaveManager.LoadSaveData += SaveManager_LoadSaveData;
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            Stage.onServerStageComplete -= Stage_onServerStageComplete;

            _effectDispatcher.OnEffectAboutToDispatchServer -= onEffectAboutToDispatchServer;

            resetAllCounters();

            SaveManager.CollectSaveData -= SaveManager_CollectSaveData;
            SaveManager.LoadSaveData -= SaveManager_LoadSaveData;
        }

        void SaveManager_CollectSaveData(ref SaveContainer container)
        {
            container.ActivationCounterData = new EffectActivationCounterData
            {
                ActivationCounts = _effectActivationCounts.Select(counter => new SerializableEffectActivationCount(counter))
                                                          .Where(c => c.RunActivations > 0)
                                                          .ToArray()
            };
        }

        void SaveManager_LoadSaveData(in SaveContainer container)
        {
            if (container.ActivationCounterData is null)
                return;

            resetAllCounters();

            if (container.ActivationCounterData.ActivationCounts is not null)
            {
                foreach (SerializableEffectActivationCount serializedActivationCount in container.ActivationCounterData.ActivationCounts)
                {
                    ChaosEffectIndex effectIndex = serializedActivationCount.Effect.EffectIndex;
                    if (effectIndex <= ChaosEffectIndex.Invalid)
                    {
                        Log.Info($"Unknown effect in save data: '{serializedActivationCount.Effect}'");
                        continue;
                    }

                    if ((int)effectIndex >= _effectActivationCounts.Length)
                    {
                        Log.Error($"Effect index '{serializedActivationCount.Effect}' out of range: i={effectIndex}, must be in the range 0<=i<{_effectActivationCounts.Length}");
                        continue;
                    }

                    serializedActivationCount.ApplyTo(ref _effectActivationCounts[(int)effectIndex]);
                }
            }
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

        void onEffectAboutToDispatchServer(ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, bool willStart)
        {
            if (!willStart || (dispatchFlags & EffectDispatchFlags.DontCount) != 0)
                return;

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

        ref ChaosEffectActivationCounter getEffectActivationCounterUncheckedRef(ChaosEffectInfo effectInfo)
        {
            return ref _effectActivationCounts[(int)effectInfo.EffectIndex];
        }

        ChaosEffectActivationCounter getEffectActivationCounter(ChaosEffectInfo effectInfo)
        {
            if (effectInfo.EffectIndex <= ChaosEffectIndex.Invalid || (int)effectInfo.EffectIndex >= _effectActivationCounts.Length)
                return ChaosEffectActivationCounter.EmptyCounter;

            return getEffectActivationCounterUncheckedRef(effectInfo);
        }

        public int GetTotalRunEffectActivationCount(ChaosEffectInfo effectInfo)
        {
            return getEffectActivationCounter(effectInfo).RunActivations;
        }

        public int GetTotalStageEffectActivationCount(ChaosEffectInfo effectInfo)
        {
            return getEffectActivationCounter(effectInfo).StageActivations;
        }

        public int GetEffectActivationCount(ChaosEffectInfo effectInfo, EffectActivationCountMode mode)
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
