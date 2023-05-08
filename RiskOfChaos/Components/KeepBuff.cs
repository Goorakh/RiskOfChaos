using RiskOfChaos.Utilities;
using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(CharacterBody))]
    public sealed class KeepBuff : MonoBehaviour
    {
        CharacterBody _body;

        public BuffIndex BuffIndex = BuffIndex.None;

        int _buffStackCount = 0;
        public int BuffStackCount
        {
            get
            {
                return _buffStackCount;
            }
            set
            {
                if (_body)
                {
                    if (value > _buffStackCount)
                    {
                        for (int i = 0; i < value - _buffStackCount; i++)
                        {
                            _body.AddBuff(BuffIndex);
                        }
                    }
                    else if (value < _buffStackCount)
                    {
                        for (int i = 0; i < _buffStackCount - value; i++)
                        {
                            _body.RemoveBuff(BuffIndex);
                        }
                    }
                    else
                    {
                        return;
                    }

                    BossUtils.TryRefreshBossTitleFor(_body);
                }

                _buffStackCount = value;
            }
        }

        void Awake()
        {
            _body = GetComponent<CharacterBody>();
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        void FixedUpdate()
        {
            if (BuffIndex == BuffIndex.None)
                return;

            if (BuffStackCount <= 0)
            {
                Destroy(this);
                return;
            }

            int currentBuffCount = _body.GetBuffCount(BuffIndex);
            if (currentBuffCount < BuffStackCount)
            {
                for (int i = 0; i < BuffStackCount - currentBuffCount; i++)
                {
                    _body.AddBuff(BuffIndex);
                }

                BossUtils.TryRefreshBossTitleFor(_body);
            }
        }

        public static void AddTo(CharacterBody body, BuffIndex buff)
        {
            BuffDef buffDef = BuffCatalog.GetBuffDef(buff);
            if (!buffDef)
                return;

            KeepBuff existingComponent = body.GetComponents<KeepBuff>().FirstOrDefault(kb => kb.BuffIndex == buff);
            if (existingComponent)
            {
                if (buffDef.canStack)
                {
                    existingComponent.BuffStackCount++;
                }

                return;
            }

            KeepBuff keepBuff = body.gameObject.AddComponent<KeepBuff>();
            keepBuff.BuffIndex = buff;
            keepBuff.BuffStackCount = 1;
        }
    }
}
