using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("remove_all_money", DefaultSelectionWeight = 0.6f, EffectWeightReductionPercentagePerActivation = 30f)]
    public sealed class RemoveAllMoney : BaseEffect
    {
        public override void OnStart()
        {
            foreach (CharacterMaster master in PlayerUtils.GetAllPlayerMasters(false))
            {
                master.money = 0;
            }
        }
    }
}
