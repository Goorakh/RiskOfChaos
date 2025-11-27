using RoR2;
using RoR2.Orbs;
using System;
using UnityEngine;

namespace RiskOfChaos.Content.Orbs
{
    public sealed class EquipmentTransferOrb : Orb
    {
        public EquipmentIndex EquipmentIndex;

        public Inventory TargetInventory;

        public Action<EquipmentTransferOrb> OnArrivalEvent = DefaultOnArrivalBehavior;

        public float TravelDuration = 1f;

        public override void Begin()
        {
            base.Begin();

            duration = TravelDuration;
            if (target)
            {
                EffectData effectData = new EffectData
                {
                    origin = origin,
                    genericFloat = duration,
                    genericUInt = Util.IntToUintPlusOne((int)EquipmentIndex)
                };

                effectData.SetHurtBoxReference(target);

                EffectManager.SpawnEffect(RoCContent.Effects.EquipmentTransferOrbEffect.index, effectData, true);
            }
        }

        public override void OnArrival()
        {
            base.OnArrival();

            OnArrivalEvent?.Invoke(this);
        }

        public static void DefaultOnArrivalBehavior(EquipmentTransferOrb orb)
        {
            if (orb.TargetInventory)
            {
                orb.TargetInventory.SetEquipmentIndex(orb.EquipmentIndex, false);
            }
        }

        public static EquipmentTransferOrb DispatchEquipmentTransferOrb(Vector3 origin, Inventory targetInventory, EquipmentIndex equipmentIndex, Action<EquipmentTransferOrb> onArrivalBehavior = null, HurtBox orbTargetOverride = null)
        {
            onArrivalBehavior ??= DefaultOnArrivalBehavior;

            EquipmentTransferOrb orb = new EquipmentTransferOrb()
            {
                origin = origin,
                TargetInventory = targetInventory,
                EquipmentIndex = equipmentIndex,
                OnArrivalEvent = onArrivalBehavior
            };

            if (orbTargetOverride)
            {
                orb.target = orbTargetOverride;
            }
            else
            {
                if (targetInventory && targetInventory.TryGetComponent(out CharacterMaster targetMaster))
                {
                    CharacterBody targetBody = targetMaster.GetBody();
                    if (targetBody)
                    {
                        orb.target = targetBody.mainHurtBox;
                    }
                }
            }

            OrbManager.instance.AddOrb(orb);

            return orb;
        }
    }
}
