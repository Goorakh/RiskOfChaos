using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("disable_recoil", 90f, AllowDuplicates = false)]
    public sealed class DisableRecoil : NetworkBehaviour, ICameraModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;

        void Start()
        {
            if (NetworkServer.active)
            {
                CameraModificationManager.Instance.RegisterModificationProvider(this);
            }
        }

        void OnDestroy()
        {
            if (CameraModificationManager.Instance)
            {
                CameraModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public void ModifyValue(ref CameraModificationData value)
        {
            value.RecoilMultiplier = Vector2.zero;
        }
    }
}
