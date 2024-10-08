using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    [Obsolete]
    public class SaveContainer
    {
        [DataMember(Name = "e")]
        public EffectsDataContainer Effects = new EffectsDataContainer();
    }
}