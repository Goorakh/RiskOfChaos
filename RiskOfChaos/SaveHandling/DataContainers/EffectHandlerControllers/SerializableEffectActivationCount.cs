using RiskOfChaos.EffectHandling;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializableEffectActivationCount
    {
        [DataMember(Name = "e")]
        public SerializableEffect Effect;

        [DataMember(Name = "ra")]
        public int RunActivations;

        public SerializableEffectActivationCount()
        {
        }

        public SerializableEffectActivationCount(ChaosEffectActivationCounter counter)
        {
            Effect = new SerializableEffect(counter.EffectIndex);
            RunActivations = counter.RunActivations;
        }

        public void ApplyTo(ref ChaosEffectActivationCounter counter)
        {
            counter.RunActivations = RunActivations;
            counter.StageActivations = 0;
        }
    }
}