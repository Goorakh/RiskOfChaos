using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos_PatcherInterop;
using RoR2;
using RoR2.Projectile;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class ProjectileInteropPatches
    {
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
                projectileManager.fireMsg.SetProcCoefficientOverridePlusOne(fireProjectileInfo.GetProcCoefficientOverridePlusOne());
                projectileManager.fireMsg.SetProcChainMask(fireProjectileInfo.procChainMask);
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
                fireProjectileInfo.SetProcCoefficientOverridePlusOne(projectileManager.fireMsg.GetProcCoefficientOverridePlusOne());
                fireProjectileInfo.procChainMask = projectileManager.fireMsg.GetProcChainMask();
            }
        }

        static void PlayerFireProjectileMessage_Serialize(On.RoR2.Projectile.ProjectileManager.PlayerFireProjectileMessage.orig_Serialize orig, MessageBase self, NetworkWriter writer)
        {
            orig(self, writer);

            ProjectileManager.PlayerFireProjectileMessage fireProjectileMessage = (ProjectileManager.PlayerFireProjectileMessage)self;
            writer.Write(fireProjectileMessage.GetProcCoefficientOverridePlusOne());
            writer.Write(fireProjectileMessage.GetProcChainMask());
        }

        static void PlayerFireProjectileMessage_Deserialize(On.RoR2.Projectile.ProjectileManager.PlayerFireProjectileMessage.orig_Deserialize orig, MessageBase self, NetworkReader reader)
        {
            orig(self, reader);

            ProjectileManager.PlayerFireProjectileMessage fireProjectileMessage = (ProjectileManager.PlayerFireProjectileMessage)self;
            fireProjectileMessage.SetProcCoefficientOverridePlusOne(reader.ReadSingle());
            fireProjectileMessage.SetProcChainMask(reader.ReadProcChainMask());
        }
    }
}
