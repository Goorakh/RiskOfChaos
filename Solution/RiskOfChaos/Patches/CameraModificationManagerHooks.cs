using MonoMod.Cil;
using RiskOfChaos.ModifierController.Camera;
using RoR2;
using RoR2.CameraModes;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class CameraModificationManagerHooks
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CameraTargetParams.AddRecoil += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After, x => x.MatchNewobj<Vector2>()))
                {
                    c.EmitDelegate(modifyRecoil);
                    static Vector2 modifyRecoil(Vector2 recoil)
                    {
                        CameraModificationManager modificationManager = CameraModificationManager.Instance;
                        if (modificationManager && modificationManager.AnyModificationActive)
                        {
                            return Vector2.Scale(recoil, modificationManager.RecoilMultiplier);
                        }
                        else
                        {
                            return recoil;
                        }
                    }
                }
                else
                {
                    Log.Error("Failed to find AddRecoil patch location");
                }
            };

            On.RoR2.CameraModes.CameraModeBase.Update += CameraModeBase_Update;

            On.RoR2.CameraModes.CameraModeBase.CollectLookInput += CameraModeBase_CollectLookInput;

            On.RoR2.CameraTargetParams.CalcParams += CameraTargetParams_CalcParams;

            PlayerInputHook.ModifyPlayerMoveInput += PlayerInputHook_ModifyPlayerMoveInput;
        }

        static void CameraModeBase_Update(On.RoR2.CameraModes.CameraModeBase.orig_Update orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.UpdateResult result)
        {
            orig(self, ref context, out result);

            CameraModificationManager modificationManager = CameraModificationManager.Instance;
            if (modificationManager && modificationManager.AnyModificationActive && context.targetInfo.target)
            {
                const float MIN_FOV = 10f;
                const float MAX_FOV = 170f;

                result.cameraState.fov = Mathf.Clamp(result.cameraState.fov * modificationManager.FovMultiplier, MIN_FOV, MAX_FOV);

                result.cameraState.rotation *= modificationManager.CameraRotationOffset;
            }
        }

        static void CameraModeBase_CollectLookInput(On.RoR2.CameraModes.CameraModeBase.orig_CollectLookInput orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.CollectLookInputResult result)
        {
            orig(self, ref context, out result);

            CameraModificationManager modificationManager = CameraModificationManager.Instance;
            if (modificationManager && modificationManager.AnyModificationActive && context.targetInfo.target)
            {
                Vector2 rotatedLookInput = modificationManager.CameraRotationOffset * result.lookInput;
                if (rotatedLookInput.sqrMagnitude > 0f)
                {
                    float lookInputMagnitude = result.lookInput.magnitude;
                    result.lookInput = rotatedLookInput.normalized * lookInputMagnitude;
                }
            }
        }

        static void CameraTargetParams_CalcParams(On.RoR2.CameraTargetParams.orig_CalcParams orig, CameraTargetParams self, out CharacterCameraParamsData dest)
        {
            orig(self, out dest);

            CameraModificationManager modificationManager = CameraModificationManager.Instance;
            if (modificationManager && modificationManager.AnyModificationActive)
            {
                float distanceMultiplier = modificationManager.CameraDistanceMultiplier;

                dest.idealLocalCameraPos.value *= distanceMultiplier;
            }
        }

        static void PlayerInputHook_ModifyPlayerMoveInput(PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput)
        {
            CameraModificationManager modificationManager = CameraModificationManager.Instance;
            if (modificationManager && modificationManager.AnyModificationActive)
            {
                const float ROTATION_TO_CONSIDER_FLIPPED = 120f;

                float zOffset = modificationManager.CameraRotationOffset.eulerAngles.z;
                if (zOffset >= ROTATION_TO_CONSIDER_FLIPPED && zOffset <= 360f - ROTATION_TO_CONSIDER_FLIPPED)
                {
                    moveInput.x *= -1;
                }
            }
        }
    }
}
