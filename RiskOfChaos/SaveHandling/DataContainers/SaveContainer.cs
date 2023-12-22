using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    public class SaveContainer
    {
        [DataMember(Name = "eas")]
        public EffectActivationSignalerData ActivationSignalerData;

        [DataMember(Name = "ed")]
        public EffectDispatcherData DispatcherData;

        [DataMember(Name = "ted")]
        public TimedEffectHandlerData TimedEffectHandlerData;

        [DataMember(Name = "e")]
        public EffectsDataContainer Effects = new EffectsDataContainer();
    }
}