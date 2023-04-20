using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    [RequireComponent(typeof(CharacterBody))]
    public sealed class KeepBuff : MonoBehaviour
    {
        CharacterBody _body;

        public BuffIndex BuffIndex = BuffIndex.None;
        public int BuffStackCount = 1;

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
        }
    }
}
