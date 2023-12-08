using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using RiskOfOptions.OptionConfigs;
using System;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("increase_recoil", 90f, AllowDuplicates = false, ConfigName = "Increase Recoil")]
    [IncompatibleEffects(typeof(DisableRecoil))]
    public sealed class IncreaseRecoil : TimedEffect, ICameraModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _recoilMultiplier =
            ConfigFactory<float>.CreateConfig("Recoil Multiplier", 7f)
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F1}X",
                                    min = 1f,
                                    max = 20f,
                                    increment = 0.5f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseRecoil>(e => e.OnValueDirty);
                                })
                                .Build();

        [EffectNameFormatArgs]
        static string[] GetEffectNameFormatArgs()
        {
            return new string[] { _recoilMultiplier.Value.ToString() };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref CameraModificationData value)
        {
            value.RecoilMultiplier *= _recoilMultiplier.Value;
        }

        public override void OnStart()
        {
            CameraModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (CameraModificationManager.Instance)
            {
                CameraModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
