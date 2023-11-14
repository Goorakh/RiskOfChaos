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
    [ChaosTimedEffect("increase_skill_stocks", TimedEffectType.UntilStageEnd, ConfigName = "Increase Skill Charges")]
    public sealed class IncreaseSkillStocks : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _stockAdds =
            ConfigFactory<int>.CreateConfig("Charges", 1)
                              .Description("The amount of charges to add to each skill")
                              .OptionConfig(new IntSliderConfig
                              {
                                  formatString = "+{0}",
                                  min = 1,
                                  max = 10
                              })
                              .OnValueChanged(() =>
                              {
                                  if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                      return;

                                  foreach (IncreaseSkillStocks instance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseSkillStocks>())
                                  {
                                      instance.OnValueDirty?.Invoke();
                                  }
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
            int addedStocks = _stockAdds.Value;
            return new object[] { addedStocks, addedStocks > 1 ? "s" : string.Empty };
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
            value.AddStocksSafe(_stockAdds.Value);
        }
    }
}
