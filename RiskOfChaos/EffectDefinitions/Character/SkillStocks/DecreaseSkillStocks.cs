using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.SkillSlots;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.SkillStocks
{
    [ChaosTimedEffect("decrease_skill_stocks", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Skill Charges")]
    public sealed class DecreaseSkillStocks : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _stocksToRemove =
            ConfigFactory<int>.CreateConfig("Charges", 1)
                              .Description("The amount of charges to remove from each skill")
                              .OptionConfig(new IntSliderConfig
                              {
                                  formatString = "-{0}",
                                  min = 1,
                                  max = 10
                              })
                              .OnValueChanged(() =>
                              {
                                  if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                      return;

                                  TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<DecreaseSkillStocks>(e => e.OnValueDirty);
                              })
                              .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance;
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            int removedStocks = _stocksToRemove.Value;
            return new object[] { removedStocks, removedStocks > 1 ? "s" : string.Empty };
        }

        public override void OnStart()
        {
            SkillSlotModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (SkillSlotModificationManager.Instance)
            {
                SkillSlotModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref SkillSlotModificationData value)
        {
            value.StockAdds -= _stocksToRemove.Value;
        }
    }
}
