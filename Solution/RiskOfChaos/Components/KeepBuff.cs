using HG;
using RiskOfChaos.EffectDefinitions.Character.Buff;
using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(CharacterBody))]
    public sealed class KeepBuff : MonoBehaviour
    {
        static readonly List<KeepBuff> _instances = [];
        public static readonly ReadOnlyCollection<KeepBuff> Instances = new ReadOnlyCollection<KeepBuff>(_instances);

        [SerializeField]
        BuffIndex _buffIndex = BuffIndex.None;
        public BuffIndex BuffIndex
        {
            get => _buffIndex;
            private set => _buffIndex = value;
        }

        public int MinBuffCount;

        BuffIndex _appliedBuffIndex = BuffIndex.None;
        int _appliedBuffCount;

        List<ApplyBuffEffect> _subscribedToEffects;

        public CharacterBody Body { get; private set; }

        void Awake()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Activated on client");
                enabled = false;
                return;
            }

            Body = GetComponent<CharacterBody>();
        }

        void OnDestroy()
        {
            if (_subscribedToEffects != null)
            {
                foreach (ApplyBuffEffect applyBuffEffect in _subscribedToEffects)
                {
                    if (applyBuffEffect)
                    {
                        applyBuffEffect.RemoveBuffsEvent -= removeBuffs;
                        applyBuffEffect.EffectComponent.OnEffectEnd -= onEffectEnd;
                    }
                }

                _subscribedToEffects = ListPool<ApplyBuffEffect>.ReturnCollection(_subscribedToEffects);
            }
        }

        void OnEnable()
        {
            _instances.Add(this);
        }

        void OnDisable()
        {
            _instances.Remove(this);
            removeAllAppliedBuffs();
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active)
            {
                Destroy(this);
                return;
            }

            if (BuffIndex == BuffIndex.None || MinBuffCount <= 0)
            {
                Destroy(this);
                return;
            }

            if (BuffIndex != _appliedBuffIndex)
            {
                if (_appliedBuffIndex != BuffIndex.None)
                {
                    removeAllAppliedBuffs();
                }

                _appliedBuffIndex = BuffIndex;
            }

            if (_appliedBuffCount > MinBuffCount)
            {
                removeAppliedBuffs(_appliedBuffCount - MinBuffCount);
            }
            else if (_appliedBuffCount < MinBuffCount)
            {
                addAppliedBuffs(MinBuffCount - _appliedBuffCount);
            }
        }

        void removeAllAppliedBuffs()
        {
            removeAppliedBuffs(_appliedBuffCount);
            _appliedBuffIndex = BuffIndex.None;
            _appliedBuffCount = 0;
        }

        void removeAppliedBuffs(int count)
        {
            if (_appliedBuffIndex != BuffIndex.None)
            {
                count = Mathf.Min(count, _appliedBuffCount);
                if (count > 0)
                {
                    _appliedBuffCount -= count;

                    if (NetworkServer.active && Body)
                    {
                        Body.SetBuffCount(_appliedBuffIndex, Mathf.Max(0, Body.GetBuffCount(_appliedBuffIndex) - count));
                    }
                }
            }
        }

        void addAppliedBuffs(int count)
        {
            if (_appliedBuffIndex != BuffIndex.None && count > 0)
            {
                _appliedBuffCount += count;

                if (NetworkServer.active && Body)
                {
                    Body.SetBuffCount(_appliedBuffIndex, Body.GetBuffCount(_appliedBuffIndex) + count);
                }
            }
        }

        public void EnsureValidBuffCount(BuffIndex buffIndex, ref int buffCount)
        {
            if (!NetworkServer.active)
                return;

            if (buffIndex != _appliedBuffIndex)
                return;
            
            buffCount = Mathf.Max(buffCount, _appliedBuffCount);
        }

        public void SubscribeToEffect(ApplyBuffEffect applyBuffEffect)
        {
            applyBuffEffect.RemoveBuffsEvent += removeBuffs;
            applyBuffEffect.EffectComponent.OnEffectEnd += onEffectEnd;

            _subscribedToEffects ??= ListPool<ApplyBuffEffect>.RentCollection();
            _subscribedToEffects.Add(applyBuffEffect);
        }

        void onEffectEnd(ChaosEffectComponent effectComponent)
        {
            effectComponent.OnEffectEnd -= onEffectEnd;

            if (effectComponent.TryGetComponent(out ApplyBuffEffect applyBuffEffect))
            {
                applyBuffEffect.RemoveBuffsEvent -= removeBuffs;
                removeBuffs(applyBuffEffect.BuffStackCount);

                _subscribedToEffects.Remove(applyBuffEffect);
            }
        }

        void removeBuffs(int count)
        {
            MinBuffCount -= count;
            if (MinBuffCount <= 0)
            {
                Destroy(this);
            }
        }

        public static KeepBuff FindKeepBuffComponent(CharacterBody body, BuffIndex buffIndex)
        {
            foreach (KeepBuff keepBuff in Instances)
            {
                if (keepBuff && keepBuff.Body == body && keepBuff.BuffIndex == buffIndex)
                {
                    return keepBuff;
                }
            }

            return null;
        }

        public static KeepBuff GetOrAddBuffComponent(CharacterBody body, BuffIndex buffIndex)
        {
            KeepBuff keepBuff = FindKeepBuffComponent(body, buffIndex);
            if (!keepBuff)
            {
                keepBuff = body.gameObject.AddComponent<KeepBuff>();
                keepBuff.BuffIndex = buffIndex;
            }

            return keepBuff;
        }
    }
}
