using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.SkillSlots
{
    public struct SkillSlotModificationData : IEquatable<SkillSlotModificationData>
    {
        public readonly SkillSlot SlotIndex;

        public bool ForceIsLocked = false;

        public SkillSlotModificationData(SkillSlot slotIndex)
        {
            SlotIndex = slotIndex;
        }

        public readonly void Serialize(NetworkWriter writer)
        {
            writer.Write((sbyte)SlotIndex);
            writer.Write(ForceIsLocked);
        }

        public static void Serialize(NetworkWriter writer, SkillSlotModificationData modificationData)
        {
            modificationData.Serialize(writer);
        }

        public static SkillSlotModificationData Deserialize(NetworkReader reader)
        {
            SkillSlotModificationData modificationData = new SkillSlotModificationData((SkillSlot)reader.ReadSByte());
            modificationData.ForceIsLocked = reader.ReadBoolean();
            return modificationData;
        }

        public override bool Equals(object obj)
        {
            return obj is SkillSlotModificationData data && Equals(data);
        }

        public bool Equals(SkillSlotModificationData other)
        {
            return SlotIndex == other.SlotIndex &&
                   ForceIsLocked == other.ForceIsLocked;
        }

        public override int GetHashCode()
        {
            int hashCode = 969723169;
            hashCode = hashCode * -1521134295 + SlotIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + ForceIsLocked.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(SkillSlotModificationData left, SkillSlotModificationData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SkillSlotModificationData left, SkillSlotModificationData right)
        {
            return !(left == right);
        }
    }
}
