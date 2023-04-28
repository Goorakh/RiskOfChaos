using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.SkillSlots
{
    public struct SkillSlotModificationData
    {
        public readonly SkillSlot SlotIndex;

        public bool ForceIsLocked = false;
        public bool ForceActivate = false;

        public SkillSlotModificationData(SkillSlot slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }
}
