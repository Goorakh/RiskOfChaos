using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Skill
{
    [ChaosTimedEffect("decrease_skill_stocks", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Skill Charges")]
    public sealed class DecreaseSkillStocks : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _stocksToRemove =
            ConfigFactory<int>.CreateConfig("Charges", 1)
                              .Description("The amount of charges to remove from each skill")
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.SkillSlotModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_PluralizedCount(_stocksToRemove);
        }

        ValueModificationController _skillSlotModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _skillSlotModificationController = Instantiate(RoCContent.NetworkedPrefabs.SkillSlotModificationProvider).GetComponent<ValueModificationController>();

                SkillSlotModificationProvider skillSlotModificationProvider = _skillSlotModificationController.GetComponent<SkillSlotModificationProvider>();
                skillSlotModificationProvider.StockAddConfigBinding.BindToConfig(_stocksToRemove, v => -v);

                NetworkServer.Spawn(_skillSlotModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_skillSlotModificationController)
            {
                _skillSlotModificationController.Retire();
                _skillSlotModificationController = null;
            }
        }
    }
}
