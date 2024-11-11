using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.SaveHandling;
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

        public ChaosEffectIndex ChaosEffectIndex = ChaosEffectIndex.Invalid;

        public ChaosEffectInfo ChaosEffectInfo => ChaosEffectCatalog.GetEffectInfo(ChaosEffectIndex);

        [NonSerialized]
        public bool EffectDestructionHandledByComponent;

        ObjectSerializationComponent _serializationComponent;

        RunTimeStamp _timeStarted;
        const uint TIME_STARTED_DIRTY_BIT = 1 << 0;

        EffectNameFormatter _instanceNameFormatter;
        const uint INSTANCE_NAME_FORMATTER_DIRTY_BIT = 1 << 1;

        IEffectHUDVisibilityProvider[] _hudVisibilityProviders;

        [SerializedMember("rng")]
        Xoroshiro128Plus _rng;

        bool _isInitialized;

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
                        Log.Error($"Effect {name} ({netId}) is missing RNG seed, generating random seed.");
                    }

                    SetRngSeedServer(RoR2Application.rng.nextUlong);
                }

                return _rng;
            }
        }

        public bool ShouldDisplayOnHUD
        {
            get
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
        }

        public EffectNameFormatter EffectNameFormatter
        {
            get
            {
                if (_instanceNameFormatter != null)
                    return _instanceNameFormatter;

                if (!ChaosEffectNameFormattersNetworker.Instance)
                    return null;

                return ChaosEffectNameFormattersNetworker.Instance.GetNameFormatter(ChaosEffectIndex);
            }
            [Server]
            set
            {
                SetSyncVar(value, ref _instanceNameFormatter, INSTANCE_NAME_FORMATTER_DIRTY_BIT);
                ChaosEffectInfo.MarkNameFormatterDirty();
            }
        }

        [Server]
        public void SetRngSeedServer(ulong seed)
        {
#if DEBUG
            Log.Debug($"{name}: Server RNG seed {seed}");
#endif

            _rng = new Xoroshiro128Plus(seed);
        }

        void Awake()
        {
            _hudVisibilityProviders = GetComponents<IEffectHUDVisibilityProvider>();
            _serializationComponent = GetComponent<ObjectSerializationComponent>();
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
                Log.Error($"Effect controller {name} ({netId}) is missing an effect index");

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

        [Server]
        public void RetireEffect()
        {
            NetworkServer.Destroy(gameObject);

#if DEBUG
            Log.Debug($"Retired effect controller {name} (id={netId})");
#endif
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

            if ((dirtyBits & INSTANCE_NAME_FORMATTER_DIRTY_BIT) != 0)
            {
                writer.Write(_instanceNameFormatter);
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

            if ((dirtyBits & INSTANCE_NAME_FORMATTER_DIRTY_BIT) != 0)
            {
                _instanceNameFormatter = reader.ReadEffectNameFormatter();
                ChaosEffectInfo.MarkNameFormatterDirty();
            }
        }
    }
}
