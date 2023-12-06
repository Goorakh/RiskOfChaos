using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController;
using RiskOfChaos.ModifierController.Camera;
using RiskOfChaos.Patches;
using RoR2;
using RoR2.CameraModes;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("flip_camera", 30f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class FlipCamera : TimedEffect, ICameraModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref CameraModificationData value)
        {
            value.RotationOffset *= Quaternion.Euler(0f, 0f, 180f);
        }

        public override void OnStart()
        {
            if (NetworkServer.active)
            {
                CameraModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }

            On.RoR2.CameraModes.CameraModeBase.CollectLookInput += CameraModeBase_CollectLookInput;

            PlayerInputHook.ModifyPlayerMoveInput += PlayerInputHook_ModifyPlayerMoveInput;
        }

        public override void OnEnd()
        {
            if (NetworkServer.active && CameraModificationManager.Instance)
            {
                CameraModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }

            On.RoR2.CameraModes.CameraModeBase.CollectLookInput -= CameraModeBase_CollectLookInput;

            PlayerInputHook.ModifyPlayerMoveInput -= PlayerInputHook_ModifyPlayerMoveInput;
        }

        static void CameraModeBase_CollectLookInput(On.RoR2.CameraModes.CameraModeBase.orig_CollectLookInput orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.CollectLookInputResult result)
        {
            orig(self, ref context, out result);

            result.lookInput *= -1;
        }

        static void PlayerInputHook_ModifyPlayerMoveInput(PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput)
        {
            moveInput.x *= -1;
        }
    }
}
