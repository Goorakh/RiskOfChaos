using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("remove_all_money", DefaultSelectionWeight = 0.6f, EffectWeightReductionPercentagePerActivation = 30f)]
    public sealed class RemoveAllMoney : BaseEffect
    {
        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
            {
                master.money = 0;
            }, Util.GetBestMasterName);
        }
    }
}
