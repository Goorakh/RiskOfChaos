using R2API.Networking.Interfaces;
using RiskOfChaos.Patches;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking
{
    public sealed class SyncLockedPlayerSkillSlots : INetMessage
    {
        public delegate void OnReceiveDelegate(bool[] lockedSkillSlots);
        public static event OnReceiveDelegate OnReceive;

        bool[] _lockedSkillSlots;

        public SyncLockedPlayerSkillSlots(bool[] lockedSkillSlots)
        {
            _lockedSkillSlots = lockedSkillSlots;
        }

        public SyncLockedPlayerSkillSlots()
        {
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.WriteBitArray(_lockedSkillSlots);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            _lockedSkillSlots = new bool[ForceLockPlayerSkillSlot.SKILL_SLOT_COUNT];
            reader.ReadBitArray(_lockedSkillSlots);
        }

        void INetMessage.OnReceived()
        {
            OnReceive?.Invoke(_lockedSkillSlots);
        }
    }
}
