using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectComponent))]
    public sealed class ApplyBuffEffect : NetworkBehaviour
    {
        static readonly List<ApplyBuffEffect> _instancesList = [];

        public static int GetActiveBuffStackCount(BuffIndex buffIndex)
        {
            int stackCount = 0;
            foreach (ApplyBuffEffect applyBuffEffect in _instancesList)
            {
                if (applyBuffEffect.BuffIndex == buffIndex)
                {
                    stackCount += applyBuffEffect.BuffStackCount;
                }
            }

            return stackCount;
        }

        public static bool CanSelectBuff(BuffIndex buffIndex)
        {
            if (buffIndex == BuffIndex.None)
                return false;

            BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
            if (!buffDef)
                return false;

            int stackCount = GetActiveBuffStackCount(buffIndex);
            return stackCount < int.MaxValue && (buffDef.canStack || stackCount == 0);
        }

        public static bool CanSelectBuff(BuffDef buff)
        {
            return buff && CanSelectBuff(buff.buffIndex);
        }

        public delegate void OnBuffAppliedDelegate(CharacterBody body);
        public event OnBuffAppliedDelegate OnBuffAppliedServer;

        public event Action OnAppliedBuffChanged;

        public event Action<int> RemoveBuffsEvent;

        [SyncVar(hook = nameof(hookSetBuffIndex))]
        int _buffIndexInternal;

        [SerializedMember("bi")]
        public BuffIndex BuffIndex
        {
            get => (BuffIndex)(_buffIndexInternal - 1);
            set => _buffIndexInternal = (int)value + 1;
        }

        [SyncVar(hook = nameof(hookSetBuffStackCount))]
        int _buffStackCount = 1;

        [SerializedMember("sc")]
        public int BuffStackCount
        {
            get => _buffStackCount;
            set => _buffStackCount = value;
        }

        public ChaosEffectComponent EffectComponent { get; private set; }

        BuffIndex _appliedBuffIndexServer = BuffIndex.None;
        int _appliedBuffStackCountServer;

        bool _appliedBuffDirty;

        void Awake()
        {
            _instancesList.Add(this);

            EffectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                CharacterBody.onBodyStartGlobal += tryAddBuff;
            }
        }

        void OnDestroy()
        {
            _instancesList.Remove(this);

            CharacterBody.onBodyStartGlobal -= tryAddBuff;

            removeAllBuffComponents();
        }

        void FixedUpdate()
        {
            if (_appliedBuffDirty)
            {
                _appliedBuffDirty = false;

                if (NetworkServer.active)
                {
                    if (_appliedBuffStackCountServer != BuffStackCount || _appliedBuffIndexServer != BuffIndex)
                    {
                        removeAllBuffComponents();

                        CharacterBody.readOnlyInstancesList.TryDo(tryAddBuff, FormatUtils.GetBestBodyName);

                        _appliedBuffIndexServer = BuffIndex;
                        _appliedBuffStackCountServer = BuffStackCount;
                    }
                }

                OnAppliedBuffChanged?.Invoke();
            }
        }

        void markAppliedBuffDirty()
        {
            _appliedBuffDirty = true;
        }

        [Server]
        void tryAddBuff(CharacterBody body)
        {
            if (!isActiveAndEnabled || BuffIndex == BuffIndex.None || BuffStackCount <= 0)
                return;

            KeepBuff keepBuff = KeepBuff.GetOrAddBuffComponent(body, BuffIndex);
            keepBuff.MinBuffCount += BuffStackCount;
            keepBuff.SubscribeToEffect(this);

            OnBuffAppliedServer?.Invoke(body);
        }

        [Server]
        void removeAllBuffComponents()
        {
            RemoveBuffsEvent?.Invoke(_appliedBuffStackCountServer);
        }

        void hookSetBuffIndex(int value)
        {
            _buffIndexInternal = value;
            markAppliedBuffDirty();
        }

        void hookSetBuffStackCount(int value)
        {
            _buffStackCount = value;
            markAppliedBuffDirty();
        }
    }
}
