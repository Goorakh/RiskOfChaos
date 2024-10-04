using HG;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectDispatcher))]
    public class ChaosEffectTracker : NetworkBehaviour
    {
        static ChaosEffectTracker _instance;
        public static ChaosEffectTracker Instance => _instance;

        [Obsolete]
        public delegate void TimedEffectStatusDelegate(TimedEffect effectInstance);

        [Obsolete]
        public static event TimedEffectStatusDelegate OnTimedEffectStartServer;
        [Obsolete]
        public static event TimedEffectStatusDelegate OnTimedEffectEndServer;
        [Obsolete]
        public static event TimedEffectStatusDelegate OnTimedEffectDirtyServer;

        readonly struct EffectInstanceInfo
        {
            public readonly ChaosEffectComponent EffectComponent;
            public readonly ChaosEffectDurationComponent DurationComponent;

            public EffectInstanceInfo(ChaosEffectComponent effectComponent)
            {
                EffectComponent = effectComponent;
                DurationComponent = EffectComponent.GetComponent<ChaosEffectDurationComponent>();
            }
        }

        struct EffectActivityInfo
        {
            public static readonly EffectActivityInfo Empty = new EffectActivityInfo
            {
                ActiveEffects = [],
                ActiveEffectsCount = 0
            };

            public EffectInstanceInfo[] ActiveEffects;
            public int ActiveEffectsCount;
        }

        EffectActivityInfo[] _effectActivity;

        ChaosEffectDispatcher _effectDispatcher;

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
                tryRegisterActiveEffect(activeEffectComponent);
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
            _effectActivity ??= new EffectActivityInfo[ChaosEffectCatalog.EffectCount];

            for (int i = 0; i < _effectActivity.Length; i++)
            {
                ref EffectActivityInfo effectActivity = ref _effectActivity[i];

                if (effectActivity.ActiveEffects == null)
                {
                    effectActivity.ActiveEffects = [];
                    effectActivity.ActiveEffectsCount = 0;
                }
                else
                {
                    ArrayUtils.Clear(effectActivity.ActiveEffects, ref effectActivity.ActiveEffectsCount);
                }
            }
        }

        void onEffectAboutToDispatchServer(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, ref bool willStart)
        {
            if (!willStart)
                return;

            TimedEffectInfo timedEffectInfo = effectInfo as TimedEffectInfo;
            if (timedEffectInfo != null && !timedEffectInfo.AllowDuplicates)
            {
                ref EffectActivityInfo effectActivity = ref _effectActivity[(int)effectInfo.EffectIndex];
                if (effectActivity.ActiveEffectsCount > 0)
                {
                    ChaosEffectDurationComponent activeEffectDurationComponent = effectActivity.ActiveEffects[0].DurationComponent;
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

            for (ChaosEffectIndex effectIndex = 0; (int)effectIndex < _effectActivity.Length; effectIndex++)
            {
                ref EffectActivityInfo effectActivity = ref _effectActivity[(int)effectIndex];
                if (effectActivity.ActiveEffectsCount > 0)
                {
                    ChaosEffectInfo activeEffectInfo = ChaosEffectCatalog.GetEffectInfo(effectIndex);
                    if (effectInfo.IncompatibleEffects.Contains(activeEffectInfo) || activeEffectInfo.IncompatibleEffects.Contains(effectInfo))
                    {
#if DEBUG
                        Log.Debug($"Ending {effectActivity.ActiveEffectsCount} timed effect(s) {activeEffectInfo} due to: incompatible effect about to start ({effectInfo})");
#endif

                        for (int i = effectActivity.ActiveEffectsCount - 1; i >= 0; i--)
                        {
                            ChaosEffectDurationComponent durationComponent = effectActivity.ActiveEffects[i].DurationComponent;
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
            tryRegisterActiveEffect(effectComponent);
        }

        void tryRegisterActiveEffect(ChaosEffectComponent effectComponent)
        {
            ChaosEffectIndex effectIndex = effectComponent.ChaosEffectIndex;
            if (effectIndex < 0 || (int)effectIndex >= _effectActivity.Length)
            {
                Log.Error($"Invalid effect index on effect {effectComponent}");
                return;
            }

            if (!effectComponent.TryGetComponent(out ChaosEffectDurationComponent effectDurationComponent))
                return;

            ref EffectActivityInfo effectActivity = ref _effectActivity[(int)effectIndex];

            ArrayUtils.ArrayAppend(ref effectActivity.ActiveEffects, ref effectActivity.ActiveEffectsCount, new EffectInstanceInfo(effectComponent));
        }

        void onEffectEndGlobal(ChaosEffectComponent effectComponent)
        {
            ChaosEffectIndex effectIndex = effectComponent.ChaosEffectIndex;
            if (effectIndex < 0 || (int)effectIndex >= _effectActivity.Length)
            {
                Log.Error($"Invalid effect index on effect {effectComponent}");
                return;
            }

            if (!effectComponent.TryGetComponent(out ChaosEffectDurationComponent effectDurationComponent))
                return;

            ref EffectActivityInfo effectActivity = ref _effectActivity[(int)effectIndex];

            for (int i = 0; i < effectActivity.ActiveEffectsCount; i++)
            {
                if (effectActivity.ActiveEffects[i].EffectComponent == effectComponent)
                {
                    ArrayUtils.ArrayRemoveAt(effectActivity.ActiveEffects, ref effectActivity.ActiveEffectsCount, i);
                    break;
                }
            }
        }

        public bool IsAnyInstanceOfEffectRelevant(TimedEffectInfo effectInfo, in EffectCanActivateContext context)
        {
            ref EffectActivityInfo effectActivity = ref _effectActivity[(int)effectInfo.EffectIndex];
            for (int i = 0; i < effectActivity.ActiveEffectsCount; i++)
            {
                ChaosEffectDurationComponent durationComponent = effectActivity.ActiveEffects[i].DurationComponent;
                if (!durationComponent ||
                    durationComponent.TimedType != TimedEffectType.FixedDuration ||
                    durationComponent.Remaining >= context.Delay)
                {
                    return true;
                }
            }

            return false;
        }

        [Obsolete]
        [Server]
        public void EndEffectServer(TimedEffect effect)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTimedEffectActive(TimedEffectInfo effectInfo)
        {
            return GetEffectStackCount(effectInfo) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEffectStackCount(TimedEffectInfo effectInfo)
        {
            return _effectActivity[(int)effectInfo.EffectIndex].ActiveEffectsCount;
        }

        public ChaosEffectComponent[] GetActiveEffects(TimedEffectInfo effectInfo)
        {
            ref EffectActivityInfo effectActivity = ref _effectActivity[(int)effectInfo.EffectIndex];
            if (effectActivity.ActiveEffectsCount <= 0)
                return [];

            ChaosEffectComponent[] activeEffectComponents = new ChaosEffectComponent[effectActivity.ActiveEffectsCount];
            for (int i = 0; i < effectActivity.ActiveEffectsCount; i++)
            {
                activeEffectComponents[i] = effectActivity.ActiveEffects[i].EffectComponent;
            }

            return activeEffectComponents;
        }

        [Obsolete]
        public IEnumerable<TimedEffect> OLD_GetActiveEffects(TimedEffectInfo effectInfo)
        {
            return [];
        }

        [Obsolete]
        public TimedEffect[] GetAllActiveEffects()
        {
            return [];
        }

        public TEffectComponent[] GetActiveEffectComponents<TEffectComponent>() where TEffectComponent : MonoBehaviour
        {
            List<TEffectComponent> componentsBuffer = new List<TEffectComponent>(8);
            List<TEffectComponent> componentsList = new List<TEffectComponent>(8);

            for (ChaosEffectIndex effectIndex = 0; (int)effectIndex < _effectActivity.Length; effectIndex++)
            {
                ref EffectActivityInfo effectActivity = ref _effectActivity[(int)effectIndex];
                for (int i = 0; i < effectActivity.ActiveEffectsCount; i++)
                {
                    ChaosEffectComponent effectComponent = effectActivity.ActiveEffects[i].EffectComponent;
                    if (effectComponent)
                    {
                        effectComponent.GetComponents(componentsBuffer);
                        if (componentsBuffer.Count > 0)
                        {
                            componentsList.AddRange(componentsBuffer);
                        }
                    }
                }
            }

            return componentsList.ToArray();
        }

        [Obsolete]
        public IEnumerable<TEffect> OLD_GetActiveEffectInstancesOfType<TEffect>() where TEffect : TimedEffect
        {
            return [];
        }

        public void InvokeEventOnAllActiveEffectComponents<TEffectComponent>(Func<TEffectComponent, Action> eventGetter) where TEffectComponent : MonoBehaviour
        {
            if (eventGetter is null)
                throw new ArgumentNullException(nameof(eventGetter));

            foreach (TEffectComponent effectComponent in GetActiveEffectComponents<TEffectComponent>())
            {
                Action action = eventGetter(effectComponent);
                action?.Invoke();
            }
        }

        [Obsolete]
        public void OLD_InvokeEventOnAllInstancesOfEffect<TEffect>(Func<TEffect, Action> eventGetter) where TEffect : TimedEffect
        {
            if (eventGetter is null)
                throw new ArgumentNullException(nameof(eventGetter));

            foreach (TEffect effectInstance in OLD_GetActiveEffectInstancesOfType<TEffect>())
            {
                Action action = eventGetter(effectInstance);
                action?.Invoke();
            }
        }

        [ConCommand(commandName = "roc_end_all_effects", flags = ConVarFlags.SenderMustBeServer, helpText = "Ends all active timed effects")]
        static void CCEndAllTimedEffects(ConCommandArgs args)
        {
            if (!NetworkServer.active || !_instance)
                return;

            for (int i = _instance._effectActivity.Length - 1; i >= 0; i--)
            {
                ref EffectActivityInfo effectActivity = ref _instance._effectActivity[i];
                for (int j = effectActivity.ActiveEffectsCount - 1; j >= 0; j--)
                {
                    ref EffectInstanceInfo effectInstanceInfo = ref effectActivity.ActiveEffects[j];

                    ChaosEffectDurationComponent durationComponent = effectInstanceInfo.DurationComponent;
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
            for (int i = _instance._effectActivity.Length - 1; i >= 0; i--)
            {
                ref EffectActivityInfo effectActivity = ref _instance._effectActivity[i];
                for (int j = effectActivity.ActiveEffectsCount - 1; j >= 0; j--)
                {
                    ref EffectInstanceInfo effectInstanceInfo = ref effectActivity.ActiveEffects[j];

                    ChaosEffectComponent effectComponent = effectInstanceInfo.EffectComponent;
                    if (effectComponent)
                    {
                        Debug.Log($"{effectComponent.ChaosEffectInfo.GetLocalDisplayName(EffectNameFormatFlags.RuntimeFormatArgs)} (Identifier={effectComponent.ChaosEffectInfo}, ID={effectComponent.netId.Value})");
                    }
                }
            }
        }

        [ConCommand(commandName = "roc_end_effect", flags = ConVarFlags.SenderMustBeServer, helpText = "Ends an effect with the specified ID, use roc_list_active_effects to get the IDs of all active effects")]
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
