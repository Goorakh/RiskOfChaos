using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModificationController.Camera;
using RiskOfChaos.Utilities.Extensions;
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

            IL.RoR2.CameraRigController.SetCameraState += CameraRigController_SetCameraState;

            On.RoR2.CameraModes.CameraModeBase.CollectLookInput += CameraModeBase_CollectLookInput;

            On.RoR2.CameraTargetParams.CalcParams += CameraTargetParams_CalcParams;

            PlayerInputHook.ModifyPlayerMoveInput += PlayerInputHook_ModifyPlayerMoveInput;
        }

        static void CameraRigController_SetCameraState(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<CameraState>(out ParameterDefinition cameraStateParameter))
            {
                Log.Error("Failed to find cameraState parameter");
                return;
            }

            VariableDefinition unmodifiedCameraStateVar = il.AddVariable<CameraState>();

            c.Emit(OpCodes.Ldarg, cameraStateParameter);
            c.Emit(OpCodes.Stloc, unmodifiedCameraStateVar);

            c.Emit(OpCodes.Ldarga, cameraStateParameter);
            c.EmitDelegate(overrideCameraState);
            static void overrideCameraState(ref CameraState cameraState)
            {
                CameraModificationManager modificationManager = CameraModificationManager.Instance;
                if (modificationManager && modificationManager.AnyModificationActive)
                {
                    const float MIN_FOV = 10f;
                    const float MAX_FOV = 170f;

                    cameraState.fov = Mathf.Clamp(cameraState.fov * modificationManager.FOVMultiplier, MIN_FOV, MAX_FOV);

                    cameraState.rotation *= modificationManager.RotationOffset;
                }
            }

            int setCurrentStatePatchCount = 0;

            while (c.TryGotoNext(MoveType.Before,
                                 x => x.MatchStfld<CameraRigController>(nameof(CameraRigController.currentCameraState))))
            {
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldloc, unmodifiedCameraStateVar);

                c.SearchTarget = SearchTarget.Next;

                setCurrentStatePatchCount++;
            }

            if (setCurrentStatePatchCount == 0)
            {
                Log.Error("Failed to find set currentCameraState patch location");
            }
            else
            {
                Log.Debug($"Found {setCurrentStatePatchCount} set currentCameraState patch location(s)");
            }
        }

        static void CameraModeBase_CollectLookInput(On.RoR2.CameraModes.CameraModeBase.orig_CollectLookInput orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.CollectLookInputResult result)
        {
            orig(self, ref context, out result);

            CameraModificationManager modificationManager = CameraModificationManager.Instance;
            if (modificationManager && modificationManager.AnyModificationActive && context.targetInfo.target)
            {
                Vector2 rotatedLookInput = modificationManager.RotationOffset * result.lookInput;
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
                float distanceMultiplier = modificationManager.DistanceMultiplier;

                dest.idealLocalCameraPos.value *= distanceMultiplier;
            }
        }

        static void PlayerInputHook_ModifyPlayerMoveInput(PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput)
        {
            CameraModificationManager modificationManager = CameraModificationManager.Instance;
            if (modificationManager && modificationManager.AnyModificationActive)
            {
                const float ROTATION_TO_CONSIDER_FLIPPED = 120f;

                float zOffset = modificationManager.RotationOffset.eulerAngles.z;
                if (zOffset >= ROTATION_TO_CONSIDER_FLIPPED && zOffset <= 360f - ROTATION_TO_CONSIDER_FLIPPED)
                {
                    moveInput.x *= -1;
                }
            }
        }
    }
}
