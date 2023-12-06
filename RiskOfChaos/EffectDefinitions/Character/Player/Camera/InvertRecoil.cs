using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using System;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("invert_recoil", 90f)]
    [IncompatibleEffects(typeof(DisableRecoil))]
    public class InvertRecoil : TimedEffect, ICameraModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public override void OnStart()
        {
            CameraModificationManager.Instance.RegisterModificationProvider(this);
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref CameraModificationData value)
        {
            value.RecoilMultiplier *= -1f;
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
