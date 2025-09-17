using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RiskOfChaos.PatcherInterop;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using RoR2BepInExPack.Utilities;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class ProjectileInteropPatches
    {
        static readonly FixedConditionalWeakTable<ProjectileManager.PlayerFireProjectileMessage, FireProjectileMessagePatcherData> _fireProjectileMessagePatcherDataTable = [];

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.Projectile.ProjectileManager.InitializeProjectile += ProjectileManager_InitializeProjectile;

            IL.RoR2.Projectile.ProjectileManager.FireProjectileClient += ProjectileManager_FireProjectileClient;
            IL.RoR2.Projectile.ProjectileManager.HandlePlayerFireProjectileInternal += ProjectileManager_HandlePlayerFireProjectileInternal;

            On.RoR2.Projectile.ProjectileManager.PlayerFireProjectileMessage.Serialize += PlayerFireProjectileMessage_Serialize;
            On.RoR2.Projectile.ProjectileManager.PlayerFireProjectileMessage.Deserialize += PlayerFireProjectileMessage_Deserialize;
        }

        static void ProjectileManager_InitializeProjectile(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<ProjectileController>(out ParameterDefinition projectileControllerParameter))
            {
                Log.Error("Failed to find ProjectileController parameter");
                return;
            }

            if (!il.Method.TryFindParameter<FireProjectileInfo>(out ParameterDefinition fireProjectileInfoParameter))
            {
                Log.Error("Failed to find FireProjectileInfo parameter");
                return;
            }

            if (!c.TryGotoNext(MoveType.AfterLabel,
                               x => x.MatchCallOrCallvirt<ProjectileController>(nameof(ProjectileController.DispatchOnInitialized))))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Emit(OpCodes.Ldarg, projectileControllerParameter);
            c.Emit(OpCodes.Ldarg, fireProjectileInfoParameter);
            c.EmitDelegate(tryApplyProcCoefficientOverride);

            static void tryApplyProcCoefficientOverride(ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
            {
                float? procCoefficientOverride = fireProjectileInfo.GetProcCoefficientOverride();
                if (procCoefficientOverride.HasValue)
                {
                    if (projectileController.procCoefficient != procCoefficientOverride.Value)
                    {
                        Log.Debug($"Overriding projectile proc coefficient for {projectileController}: {projectileController.procCoefficient}->{procCoefficientOverride.Value}");
                        projectileController.procCoefficient = procCoefficientOverride.Value;
                    }
                }
            }
        }

        static void ProjectileManager_FireProjectileClient(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<FireProjectileInfo>(out ParameterDefinition fireProjectileInfoParameter))
            {
                Log.Error("Failed to find FireProjectileInfo parameter");
                return;
            }

            if (!c.TryGotoNext(MoveType.After, x => x.MatchStfld(out FieldReference field) && field.DeclaringType.Is(typeof(ProjectileManager.PlayerFireProjectileMessage))))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg, fireProjectileInfoParameter);
            c.EmitDelegate(setFireMsgProcCoefficientOverride);

            static void setFireMsgProcCoefficientOverride(ProjectileManager projectileManager, FireProjectileInfo fireProjectileInfo)
            {
                FireProjectileMessagePatcherData fireMessageData = _fireProjectileMessagePatcherDataTable.GetOrCreateValue(projectileManager.fireMsg);

                fireMessageData.ProcCoefficientOverride = fireProjectileInfo.GetProcCoefficientOverride();
                fireMessageData.ProcChainMask = fireProjectileInfo.procChainMask;
            }
        }

        static void ProjectileManager_HandlePlayerFireProjectileInternal(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After,
                               x => x.MatchInitobj<FireProjectileInfo>()))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            int fireProjectileInfoLocalIndex = -1;
            if (!c.Clone().TryGotoPrev(x => x.MatchLdloca(out fireProjectileInfoLocalIndex)))
            {
                Log.Error("Failed to find FireProjectileInfo local index");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, fireProjectileInfoLocalIndex);
            c.EmitDelegate(setProjectileInfoProcCoefficientOverrideFromFireMsg);

            static void setProjectileInfoProcCoefficientOverrideFromFireMsg(ProjectileManager projectileManager, ref FireProjectileInfo fireProjectileInfo)
            {
                if (_fireProjectileMessagePatcherDataTable.TryGetValue(projectileManager.fireMsg, out FireProjectileMessagePatcherData fireProjectileMessageData))
                {
                    fireProjectileInfo.SetProcCoefficientOverride(fireProjectileMessageData.ProcCoefficientOverride);

                    ProcChainMask fireMsgProcChainMask = fireProjectileMessageData.ProcChainMask;
                    if (fireMsgProcChainMask.HasAnyProc())
                    {
                        fireProjectileInfo.procChainMask.AddProcsFrom(fireMsgProcChainMask);

                        Log.Debug($"Added procs {fireMsgProcChainMask} to fireProjectileInfo {fireProjectileInfo.projectilePrefab} from client message (resulting: {fireProjectileInfo.procChainMask})");
                    }
                }
            }
        }

        static void PlayerFireProjectileMessage_Serialize(On.RoR2.Projectile.ProjectileManager.PlayerFireProjectileMessage.orig_Serialize orig, ProjectileManager.PlayerFireProjectileMessage self, NetworkWriter writer)
        {
            orig(self, writer);

            float? procCoefficientOverride = null;
            ProcChainMask procChainMask = default;
            if (_fireProjectileMessagePatcherDataTable.TryGetValue(self, out FireProjectileMessagePatcherData fireProjectileMessageData))
            {
                procCoefficientOverride = fireProjectileMessageData.ProcCoefficientOverride;
                procChainMask = fireProjectileMessageData.ProcChainMask;
            }

            writer.Write(InteropUtils.EncodePackedOverrideValue(procCoefficientOverride));
            writer.Write(procChainMask);
        }

        static void PlayerFireProjectileMessage_Deserialize(On.RoR2.Projectile.ProjectileManager.PlayerFireProjectileMessage.orig_Deserialize orig, ProjectileManager.PlayerFireProjectileMessage self, NetworkReader reader)
        {
            orig(self, reader);

            FireProjectileMessagePatcherData fireProjectileMessageData = _fireProjectileMessagePatcherDataTable.GetOrCreateValue(self);

            fireProjectileMessageData.ProcCoefficientOverride = InteropUtils.DecodePackedOverrideValue(reader.ReadSingle());
            fireProjectileMessageData.ProcChainMask = reader.ReadProcChainMask();
        }

        class FireProjectileMessagePatcherData
        {
            public ProcChainMask ProcChainMask;
            public float? ProcCoefficientOverride;
        }
    }
}
