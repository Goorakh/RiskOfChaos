using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.SaveHandling.DataContainers.Effects;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers
{
    [Serializable]
    [Obsolete]
    public class EffectsDataContainer
    {
        [DataMember(Name = ForceAllItemsIntoRandomItem.EFFECT_IDENTIFIER)]
        public ForceAllItemsIntoRandomItem_Data ForceAllItemsIntoRandomItem_Data;

        [DataMember(Name = RandomDifficulty.EFFECT_IDENTIFIER)]
        public RandomDifficulty_Data RandomDifficulty_Data;

        [DataMember(Name = SuppressRandomItem.EFFECT_IDENTIFIER)]
        public SuppressRandomItem_Data SuppressRandomItem_Data;
    }
}