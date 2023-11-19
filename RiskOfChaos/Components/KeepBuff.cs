using RiskOfChaos.Utilities;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(CharacterBody))]
    public sealed class KeepBuff : MonoBehaviour
    {
        CharacterBody _body;

        public BuffIndex BuffIndex = BuffIndex.None;

        uint _buffStackCount = 0;
        public uint BuffStackCount
        {
            get
            {
                return _buffStackCount;
            }
            set
            {
                if (_body)
                {
                    int currentBuffCount = _body.GetBuffCount(BuffIndex);

                    int newBuffCount;
                    if (value > _buffStackCount)
                    {
                        newBuffCount = ClampedConversion.Int32(currentBuffCount + (value - _buffStackCount));
                    }
                    else if (value < _buffStackCount)
                    {
                        newBuffCount = ClampedConversion.Int32(currentBuffCount - (_buffStackCount - value));
                    }
                    else
                    {
                        return;
                    }

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    _body.SetBuffCount(BuffIndex, newBuffCount);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                    onAppliedBuffStacksChanged();
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
            if (BuffIndex == BuffIndex.None || !NetworkServer.active)
                return;

            if (BuffStackCount <= 0)
            {
                Destroy(this);
                return;
            }

            int currentBuffCount = _body.GetBuffCount(BuffIndex);
            if (currentBuffCount < BuffStackCount)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                _body.SetBuffCount(BuffIndex, ClampedConversion.Int32(BuffStackCount));
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                onAppliedBuffStacksChanged();
            }
        }

        void onAppliedBuffStacksChanged()
        {
            // Refresh some of the elite buffs
            if (_body.inventory)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                _body.inventory.HandleInventoryChanged();
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
            else
            {
                // Make sure boss title is refreshed even if this body has no inventory
                BossUtils.TryRefreshBossTitleFor(_body);
            }
        }

        public override string ToString()
        {
            if (!_body)
            {
                return base.ToString();
            }
            else
            {
                return string.Format($"{nameof(KeepBuff)} ({{0}})", FormatUtils.GetBestBodyName(_body));
            }
        }

        public static void AddTo(CharacterBody body, BuffIndex buff, uint buffCount = 1)
        {
            BuffDef buffDef = BuffCatalog.GetBuffDef(buff);
            if (!buffDef)
                return;

            KeepBuff existingComponent = body.GetComponents<KeepBuff>().FirstOrDefault(kb => kb.BuffIndex == buff);
            if (existingComponent)
            {
                if (buffDef.canStack)
                {
                    existingComponent.BuffStackCount += buffCount;
                }

                return;
            }

            KeepBuff keepBuff = body.gameObject.AddComponent<KeepBuff>();
            keepBuff.BuffIndex = buff;
            keepBuff.BuffStackCount = buffCount;
        }
    }
}
