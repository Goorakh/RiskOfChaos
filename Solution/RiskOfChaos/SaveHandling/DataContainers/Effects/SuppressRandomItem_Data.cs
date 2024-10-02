using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.Effects
{
    [Serializable]
    public class SuppressRandomItem_Data
    {
        [DataMember(Name = "si")]
        public string[] SuppressedItems;
    }
}
