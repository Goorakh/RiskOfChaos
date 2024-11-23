using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectComponent))]
    public sealed class ApplyBuffEffect : MonoBehaviour
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

        public static IEnumerable<BuffIndex> FilterSelectableBuffs(IEnumerable<BuffIndex> buffIndices)
        {
            return buffIndices.Where(CanSelectBuff);
        }

        public delegate void OnBuffAppliedDelegate(CharacterBody body);
        public event OnBuffAppliedDelegate OnBuffAppliedServer;

        BuffIndex _buffIndex = BuffIndex.None;

        [SerializedMember("bi")]
        public BuffIndex BuffIndex
        {
            get
            {
                return _buffIndex;
            }
            set
            {
                if (_buffIndex == value)
                    return;

                _buffIndex = value;
                _buffComponentsDirty = true;
            }
        }

        int _buffStackCount = 1;

        [SerializedMember("sc")]
        public int BuffStackCount
        {
            get
            {
                return _buffStackCount;
            }
            set
            {
                if (_buffStackCount == value)
                    return;

                _buffStackCount = value;
                _buffComponentsDirty = true;
            }
        }

        readonly List<KeepBuff> _keepBuffComponents = [];
        readonly List<OnDestroyCallback> _destroyCallbacks = [];

        bool _buffComponentsDirty;

        bool _trackedObjectDestroyed;

        void Awake()
        {
            _instancesList.Add(this);
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _keepBuffComponents.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);

                CharacterBody.readOnlyInstancesList.TryDo(tryAddBuff, FormatUtils.GetBestBodyName);
                CharacterBody.onBodyStartGlobal += tryAddBuff;
            }
        }

        void OnDestroy()
        {
            _instancesList.Remove(this);

            CharacterBody.onBodyStartGlobal -= tryAddBuff;

            foreach (OnDestroyCallback destroyCallback in _destroyCallbacks)
            {
                if (destroyCallback)
                {
                    OnDestroyCallback.RemoveCallback(destroyCallback);
                }
            }

            _destroyCallbacks.Clear();

            foreach (KeepBuff keepBuff in _keepBuffComponents)
            {
                if (keepBuff)
                {
                    Destroy(keepBuff);
                }
            }

            _keepBuffComponents.Clear();
        }

        void FixedUpdate()
        {
            if (_trackedObjectDestroyed)
            {
                _trackedObjectDestroyed = false;
                UnityObjectUtils.RemoveAllDestroyed(_destroyCallbacks);

                int removedBuffComponents = UnityObjectUtils.RemoveAllDestroyed(_keepBuffComponents);

                Log.Debug($"Cleared {removedBuffComponents} destroyed buff component(s)");
            }

            if (NetworkServer.active)
            {
                if (_buffComponentsDirty)
                {
                    _buffComponentsDirty = false;
                    updateAllBuffComponents();
                }
            }
        }

        void tryAddBuff(CharacterBody body)
        {
            if (!NetworkServer.active || !isActiveAndEnabled || BuffIndex == BuffIndex.None || BuffStackCount <= 0)
                return;

            KeepBuff keepBuff = body.gameObject.AddComponent<KeepBuff>();
            updateBuffComponent(keepBuff);

            _keepBuffComponents.Add(keepBuff);

            OnDestroyCallback destroyCallback = OnDestroyCallback.AddCallback(keepBuff.gameObject, _ =>
            {
                _trackedObjectDestroyed = true;
            });

            _destroyCallbacks.Add(destroyCallback);

            OnBuffAppliedServer?.Invoke(body);
        }

        void updateAllBuffComponents()
        {
            if (!NetworkServer.active)
                return;

            foreach (KeepBuff keepBuff in _keepBuffComponents)
            {
                if (keepBuff)
                {
                    updateBuffComponent(keepBuff);
                }
            }
        }

        void updateBuffComponent(KeepBuff keepBuff)
        {
            keepBuff.BuffIndex = BuffIndex;
            keepBuff.MinBuffCount = BuffStackCount;
            keepBuff.enabled = isActiveAndEnabled && keepBuff.BuffIndex != BuffIndex.None && keepBuff.MinBuffCount > 0;
        }
    }
}
