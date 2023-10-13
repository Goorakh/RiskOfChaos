using RiskOfChaos.EffectHandling;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializableEffectActivationCount
    {
        [DataMember(Name = "ei")]
        public string EffectIdentifier;

        [DataMember(Name = "ra")]
        public int RunActivations;

        public SerializableEffectActivationCount()
        {
        }

        public SerializableEffectActivationCount(ChaosEffectActivationCounter counter)
        {
            EffectIdentifier = ChaosEffectCatalog.GetEffectInfo(counter.EffectIndex).Identifier;
            RunActivations = counter.RunActivations;
        }

        public void ApplyTo(ref ChaosEffectActivationCounter counter)
        {
            counter.RunActivations = RunActivations;
            counter.StageActivations = 0;
        }
    }
}