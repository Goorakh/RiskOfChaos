using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    [RequireComponent(typeof(CharacterBody))]
    public sealed class KeepBuff : MonoBehaviour
    {
        CharacterBody _body;

        public BuffIndex Buff = BuffIndex.None;
        public int StackCount = 1;

        void Awake()
        {
            _body = GetComponent<CharacterBody>();
        }

        void FixedUpdate()
        {
            if (Buff == BuffIndex.None)
                return;

            int currentBuffCount = _body.GetBuffCount(Buff);
            if (currentBuffCount < StackCount)
            {
                for (int i = 0; i < StackCount - currentBuffCount; i++)
                {
                    _body.AddBuff(Buff);
                }
            }
        }

        public static void AddTo(CharacterBody body, BuffIndex buff)
        {
            BuffDef buffDef = BuffCatalog.GetBuffDef(buff);
            if (!buffDef)
                return;

            KeepBuff existingComponent = body.GetComponents<KeepBuff>().FirstOrDefault(kb => kb.Buff == buff);
            if (existingComponent)
            {
                if (buffDef.canStack)
                {
                    existingComponent.StackCount++;
                }

                return;
            }

            KeepBuff keepBuff = body.gameObject.AddComponent<KeepBuff>();
            keepBuff.Buff = buff;
        }
    }
}
