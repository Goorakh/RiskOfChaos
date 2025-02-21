﻿using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    [DisallowMultipleComponent]
    public sealed class ChaosEffectComponent : NetworkBehaviour
    {
        public delegate void EffectDelegate(ChaosEffectComponent effectComponent);
        public static event EffectDelegate OnEffectStartGlobal;
        public static event EffectDelegate OnEffectEndGlobal;

        static readonly List<ChaosEffectComponent> _instances = [];
        public static readonly ReadOnlyCollection<ChaosEffectComponent> Instances = new ReadOnlyCollection<ChaosEffectComponent>(_instances);

        const uint TIME_STARTED_DIRTY_BIT = 1 << 0;

        public ChaosEffectIndex ChaosEffectIndex = ChaosEffectIndex.Invalid;

        public ChaosEffectInfo ChaosEffectInfo => ChaosEffectCatalog.GetEffectInfo(ChaosEffectIndex);

        [NonSerialized]
        public bool EffectDestructionHandledByComponent;

        ObjectSerializationComponent _serializationComponent;

        RunTimeStamp _timeStarted;

        IEffectHUDVisibilityProvider[] _hudVisibilityProviders;

        [SerializedMember("rng")]
        Xoroshiro128Plus _rng;

        bool _isInitialized;

        float _shouldDisplayOnHUDRefreshTimer;

        [SerializedMember("ts")]
        public RunTimeStamp TimeStarted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _timeStarted;

            [Server]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetSyncVar(value.ConvertTo(RunTimerType.Realtime), ref _timeStarted, TIME_STARTED_DIRTY_BIT);
        }

        public Xoroshiro128Plus Rng
        {
            [Server]
            get
            {
                if (_rng == null)
                {
                    // If we are deserialized from save data, rng may be accessed before it has been properly set from the save data
                    // In this case, this situation is harmless, no need to print an error :)
                    if (!_serializationComponent || !_serializationComponent.IsLoadedFromSave)
                    {
                        Log.Error($"Effect {Util.GetGameObjectHierarchyName(gameObject)} ({netId}) is missing RNG seed, generating random seed.");
                    }

                    SetRngSeedServer(RoR2Application.rng.nextUlong);
                }

                return _rng;
            }
        }

        bool _shouldDisplayOnHUD;
        public bool ShouldDisplayOnHUD
        {
            get
            {
                return _shouldDisplayOnHUD;
            }
            set
            {
                if (_shouldDisplayOnHUD == value)
                    return;

                _shouldDisplayOnHUD = value;
                OnShouldDisplayOnHUDChanged?.Invoke(this);
            }
        }

        public ChaosEffectDurationComponent DurationComponent { get; private set; }

        public event Action<ChaosEffectComponent> OnShouldDisplayOnHUDChanged;

        [Server]
        public void SetRngSeedServer(ulong seed)
        {
            Log.Debug($"{Util.GetGameObjectHierarchyName(gameObject)}: Server RNG seed {seed}");

            _rng = new Xoroshiro128Plus(seed);
        }

        void Awake()
        {
            _hudVisibilityProviders = GetComponents<IEffectHUDVisibilityProvider>();
            _serializationComponent = GetComponent<ObjectSerializationComponent>();
            DurationComponent = GetComponent<ChaosEffectDurationComponent>();
        }

        void OnEnable()
        {
            RefreshShouldDisplayOnHUD();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            tryInitialize();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            tryInitialize();
        }
        
        void tryInitialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            if (ChaosEffectIndex == ChaosEffectIndex.Invalid)
            {
                Log.Error($"Effect controller {Util.GetGameObjectHierarchyName(gameObject)} ({netId}) is missing an effect index");

                if (NetworkServer.active)
                {
                    RetireEffect();
                }

                return;
            }

            _instances.Add(this);
            OnEffectStartGlobal?.Invoke(this);
        }

        void OnDestroy()
        {
            if (_instances.Remove(this))
            {
                OnEffectEndGlobal?.Invoke(this);
            }
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                fixedUpdateServer();
            }

            _shouldDisplayOnHUDRefreshTimer -= Time.fixedDeltaTime;
            if (_shouldDisplayOnHUDRefreshTimer <= 0f)
            {
                _shouldDisplayOnHUDRefreshTimer = 0.5f;
                RefreshShouldDisplayOnHUD();
            }
        }

        [Server]
        void fixedUpdateServer()
        {
            if (!EffectDestructionHandledByComponent)
            {
                const float TIMEOUT_DURATION = 3f;
                if (TimeStarted.TimeSinceClamped > TIMEOUT_DURATION)
                {
                    RetireEffect();
                }
            }
        }

        public void RefreshShouldDisplayOnHUD()
        {
            ShouldDisplayOnHUD = calculateShouldDisplayOnHUD();
        }

        bool calculateShouldDisplayOnHUD()
        {
            if (!isActiveAndEnabled || ChaosEffectInfo == null)
                return false;

            foreach (IEffectHUDVisibilityProvider hudVisibilityProvider in _hudVisibilityProviders)
            {
                MonoBehaviour hudVisibilityProviderComponent = hudVisibilityProvider as MonoBehaviour;
                if (hudVisibilityProviderComponent && hudVisibilityProviderComponent.isActiveAndEnabled && !hudVisibilityProvider.CanShowOnHUD)
                {
                    return false;
                }
            }

            return true;
        }

        [Server]
        public void RetireEffect()
        {
            NetworkServer.Destroy(gameObject);

            Log.Debug($"Retired effect controller {Util.GetGameObjectHierarchyName(gameObject)} (id={netId})");
        }

        public bool IsRelevantForContext(in EffectCanActivateContext context)
        {
            if (DurationComponent)
            {
                if (DurationComponent.TimedType == TimedEffectType.FixedDuration)
                {
                    return DurationComponent.Remaining >= context.ActivationTime.TimeUntilClamped;
                }
            }

            return true;
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            uint dirtyBits = initialState ? ~0b0U : syncVarDirtyBits;
            if (!initialState)
            {
                writer.WritePackedUInt32(dirtyBits);
            }

            bool anythingWritten = false;

            if ((dirtyBits & TIME_STARTED_DIRTY_BIT) != 0)
            {
                writer.Write(_timeStarted.ConvertTo(RunTimerType.Realtime).Time);
                anythingWritten = true;
            }

            return anythingWritten || initialState;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            uint dirtyBits = initialState ? ~0b0U : reader.ReadPackedUInt32();

            if ((dirtyBits & TIME_STARTED_DIRTY_BIT) != 0)
            {
                _timeStarted = new RunTimeStamp(RunTimerType.Realtime, reader.ReadSingle());
            }
        }
    }
}
