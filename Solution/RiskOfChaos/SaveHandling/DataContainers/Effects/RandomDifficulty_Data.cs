using RoR2;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.Effects
{
    [Serializable]
    public class RandomDifficulty_Data
    {
        [DataMember(Name = "pd")]
        public DifficultyIndex[] PreviousDifficulties;
    }
}
