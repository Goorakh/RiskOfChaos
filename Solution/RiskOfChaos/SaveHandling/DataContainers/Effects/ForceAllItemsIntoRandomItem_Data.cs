using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.Effects
{
    [Serializable]
    [Obsolete]
    public class ForceAllItemsIntoRandomItem_Data
    {
        [DataMember(Name = "pni_rng")]
        public SerializableRng PickNextItemRNG;

        [DataMember(Name = "cp")]
        public string CurrentPickupName;
    }
}
