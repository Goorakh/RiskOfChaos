using HG;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectDispatcher))]
    public class ChaosEffectTracker : MonoBehaviour
    {
        static ChaosEffectTracker _instance;
        public static ChaosEffectTracker Instance => _instance;

        public delegate void TimedEffectDelegate(ChaosEffectComponent effectComponent);

        public static event TimedEffectDelegate OnTimedEffectStartGlobal;
        public static event TimedEffectDelegate OnTimedEffectEndGlobal;

        readonly struct EffectInstanceInfo
        {
            public readonly ChaosEffectComponent EffectComponent;

            public EffectInstanceInfo(ChaosEffectComponent effectComponent)
            {
                EffectComponent = effectComponent;
            }
        }

        struct TimedEffectActivityInfo
        {
            public EffectInstanceInfo[] Instances;
            public int InstancesCount;
        }

        TimedEffectActivityInfo[] _timedEffectActivity;
        readonly List<ChaosEffectComponent> _allActiveTimedEffects;
        public readonly ReadOnlyCollection<ChaosEffectComponent> AllActiveTimedEffects;

        ChaosEffectDispatcher _effectDispatcher;

        ChaosEffectTracker()
        {
            _allActiveTimedEffects = [];
            AllActiveTimedEffects = _allActiveTimedEffects.AsReadOnly();
        }

        void Awake()
        {
            _effectDispatcher = GetComponent<ChaosEffectDispatcher>();
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _effectDispatcher.OnEffectAboutToDispatchServer += onEffectAboutToDispatchServer;

            ChaosEffectComponent.OnEffectStartGlobal += onEffectStartGlobal;
            ChaosEffectComponent.OnEffectEndGlobal += onEffectEndGlobal;

            resetEffectActivity();

            foreach (ChaosEffectComponent activeEffectComponent in ChaosEffectComponent.Instances)
            {
                tryRegisterActiveTimedEffect(activeEffectComponent);
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            _effectDispatcher.OnEffectAboutToDispatchServer -= onEffectAboutToDispatchServer;

            ChaosEffectComponent.OnEffectStartGlobal -= onEffectStartGlobal;
            ChaosEffectComponent.OnEffectEndGlobal -= onEffectEndGlobal;

            resetEffectActivity();
        }

        void resetEffectActivity()
        {
            _allActiveTimedEffects.Clear();

            _timedEffectActivity ??= new TimedEffectActivityInfo[ChaosEffectCatalog.EffectCount];

            for (int i = 0; i < _timedEffectActivity.Length; i++)
            {
                ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[i];

                effectActivity.Instances ??= [];
                effectActivity.InstancesCount = 0;
            }
        }

        void onEffectAboutToDispatchServer(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, ref bool willStart)
        {
            if (!willStart)
                return;

            TimedEffectInfo timedEffectInfo = effectInfo as TimedEffectInfo;
            if (timedEffectInfo != null && !timedEffectInfo.AllowDuplicates)
            {
                ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectInfo.EffectIndex];
                if (effectActivity.InstancesCount > 0)
                {
                    ChaosEffectDurationComponent activeEffectDurationComponent = effectActivity.Instances[0].EffectComponent.DurationComponent;
                    if (activeEffectDurationComponent)
                    {
                        if (timedEffectInfo.GetCanStack(activeEffectDurationComponent.TimedType))
                        {
                            activeEffectDurationComponent.Duration += timedEffectInfo.GetDuration(activeEffectDurationComponent.TimedType);
                        }
                    }

                    willStart = false;
                    return;
                }
            }

            for (ChaosEffectIndex effectIndex = 0; (int)effectIndex < _timedEffectActivity.Length; effectIndex++)
            {
                ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectIndex];
                if (effectActivity.InstancesCount > 0)
                {
                    ChaosEffectInfo activeEffectInfo = ChaosEffectCatalog.GetEffectInfo(effectIndex);
                    if (effectInfo.IncompatibleEffects.Contains(activeEffectInfo) || activeEffectInfo.IncompatibleEffects.Contains(effectInfo))
                    {
                        Log.Debug($"Ending {effectActivity.InstancesCount} timed effect(s) {activeEffectInfo} due to: incompatible effect about to start ({effectInfo})");

                        for (int i = effectActivity.InstancesCount - 1; i >= 0; i--)
                        {
                            ChaosEffectDurationComponent durationComponent = effectActivity.Instances[i].EffectComponent.DurationComponent;
                            if (durationComponent)
                            {
                                durationComponent.EndEffect();
                            }
                        }
                    }
                }
            }
        }

        void onEffectStartGlobal(ChaosEffectComponent effectComponent)
        {
            tryRegisterActiveTimedEffect(effectComponent);
        }

        void tryRegisterActiveTimedEffect(ChaosEffectComponent effectComponent)
        {
            if (_allActiveTimedEffects.Contains(effectComponent))
            {
                Log.Error($"Attempted to registed duplicate timed effect {effectComponent} ({effectComponent.netId})");
                return;
            }

            ChaosEffectIndex effectIndex = effectComponent.ChaosEffectIndex;
            if (effectIndex < 0 || (int)effectIndex >= _timedEffectActivity.Length)
            {
                Log.Error($"Invalid effect index on effect {effectComponent}");
                return;
            }

            if (!effectComponent.DurationComponent)
                return;

            ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectIndex];

            ArrayUtils.ArrayAppend(ref effectActivity.Instances, ref effectActivity.InstancesCount, new EffectInstanceInfo(effectComponent));

            _allActiveTimedEffects.Add(effectComponent);

            OnTimedEffectStartGlobal?.Invoke(effectComponent);
        }

        void onEffectEndGlobal(ChaosEffectComponent effectComponent)
        {
            ChaosEffectIndex effectIndex = effectComponent.ChaosEffectIndex;
            if (effectIndex < 0 || (int)effectIndex >= _timedEffectActivity.Length)
            {
                Log.Error($"Invalid effect index on effect {effectComponent}");
                return;
            }

            if (!effectComponent.DurationComponent)
                return;

            ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectIndex];

            for (int i = 0; i < effectActivity.InstancesCount; i++)
            {
                if (effectActivity.Instances[i].EffectComponent == effectComponent)
                {
                    ArrayUtils.ArrayRemoveAt(effectActivity.Instances, ref effectActivity.InstancesCount, i);
                    break;
                }
            }

            _allActiveTimedEffects.Remove(effectComponent);

            OnTimedEffectEndGlobal?.Invoke(effectComponent);
        }

        public bool IsAnyInstanceOfTimedEffectRelevantForContext(TimedEffectInfo effectInfo, in EffectCanActivateContext context)
        {
            ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectInfo.EffectIndex];
            for (int i = 0; i < effectActivity.InstancesCount; i++)
            {
                if (effectActivity.Instances[i].EffectComponent.IsRelevantForContext(context))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTimedEffectActive(TimedEffectInfo effectInfo)
        {
            return effectInfo != null && GetTimedEffectStackCount(effectInfo) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetTimedEffectStackCount(TimedEffectInfo effectInfo)
        {
            return effectInfo != null ? _timedEffectActivity[(int)effectInfo.EffectIndex].InstancesCount : 0;
        }

        public ChaosEffectComponent[] GetActiveTimedEffects(TimedEffectInfo effectInfo)
        {
            if (effectInfo == null)
                return [];

            ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectInfo.EffectIndex];
            if (effectActivity.InstancesCount <= 0)
                return [];

            ChaosEffectComponent[] activeEffectComponents = new ChaosEffectComponent[effectActivity.InstancesCount];
            for (int i = 0; i < effectActivity.InstancesCount; i++)
            {
                activeEffectComponents[i] = effectActivity.Instances[i].EffectComponent;
            }

            return activeEffectComponents;
        }

        public ChaosEffectComponent GetFirstActiveTimedEffect(TimedEffectInfo effectInfo)
        {
            if (effectInfo == null)
                return null;

            ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectInfo.EffectIndex];
            if (effectActivity.InstancesCount <= 0)
                return null;

            return effectActivity.Instances[0].EffectComponent;
        }

        public TEffectComponent[] GetActiveTimedEffectComponents<TEffectComponent>() where TEffectComponent : MonoBehaviour
        {
            List<TEffectComponent> componentsBuffer = new List<TEffectComponent>(8);
            List<TEffectComponent> componentsList = new List<TEffectComponent>(_allActiveTimedEffects.Count * 2);

            for (ChaosEffectIndex effectIndex = 0; (int)effectIndex < _timedEffectActivity.Length; effectIndex++)
            {
                ref TimedEffectActivityInfo effectActivity = ref _timedEffectActivity[(int)effectIndex];
                for (int i = 0; i < effectActivity.InstancesCount; i++)
                {
                    ChaosEffectComponent effectComponent = effectActivity.Instances[i].EffectComponent;
                    if (effectComponent)
                    {
                        componentsBuffer.Clear();
                        effectComponent.GetComponents(componentsBuffer);
                        if (componentsBuffer.Count > 0)
                        {
                            componentsList.AddRange(componentsBuffer);
                        }
                    }
                }
            }

            return [.. componentsList];
        }

        [ConCommand(commandName = "roc_end_all_effects", flags = ConVarFlagUtil.SERVER, helpText = "Ends all active timed effects")]
        static void CCEndAllTimedEffects(ConCommandArgs args)
        {
            if (!NetworkServer.active || !_instance)
                return;

            for (int i = _instance._timedEffectActivity.Length - 1; i >= 0; i--)
            {
                ref TimedEffectActivityInfo effectActivity = ref _instance._timedEffectActivity[i];
                for (int j = effectActivity.InstancesCount - 1; j >= 0; j--)
                {
                    ref EffectInstanceInfo effectInstanceInfo = ref effectActivity.Instances[j];

                    ChaosEffectDurationComponent durationComponent = effectInstanceInfo.EffectComponent.DurationComponent;
                    if (durationComponent && durationComponent.TimedType != TimedEffectType.AlwaysActive)
                    {
                        durationComponent.EndEffect();
                    }
                }
            }
        }

        [ConCommand(commandName = "roc_list_active_effects", helpText = "Prints all active effects to the console")]
        static void CCListActiveEffects(ConCommandArgs args)
        {
            for (int i = _instance._timedEffectActivity.Length - 1; i >= 0; i--)
            {
                ref TimedEffectActivityInfo effectActivity = ref _instance._timedEffectActivity[i];
                for (int j = effectActivity.InstancesCount - 1; j >= 0; j--)
                {
                    ref EffectInstanceInfo effectInstanceInfo = ref effectActivity.Instances[j];

                    ChaosEffectComponent effectComponent = effectInstanceInfo.EffectComponent;
                    if (effectComponent)
                    {
                        Debug.Log($"{effectComponent.ChaosEffectInfo.GetStaticDisplayName(EffectNameFormatFlags.RuntimeFormatArgs)} (Identifier={effectComponent.ChaosEffectInfo}, ID={effectComponent.netId.Value})");
                    }
                }
            }
        }

        [ConCommand(commandName = "roc_end_effect", flags = ConVarFlagUtil.SERVER, helpText = "Ends an effect with the specified ID, use roc_list_active_effects to get the IDs of all active effects")]
        static void CCEndTimedEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance)
                return;

            args.CheckArgumentCount(1);

            uint instanceId = ClampedConversion.UInt32(args.GetArgULong(0));

            GameObject obj = Util.FindNetworkObject(new NetworkInstanceId(instanceId));
            ChaosEffectDurationComponent durationComponent = obj ? obj.GetComponent<ChaosEffectDurationComponent>() : null;
            if (!durationComponent)
            {
                Debug.Log($"Invalid effect ID {instanceId}");
                return;
            }

            durationComponent.EndEffect();
        }
    }
}
