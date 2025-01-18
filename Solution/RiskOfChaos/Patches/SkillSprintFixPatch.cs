using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RiskOfChaos.EffectUtils.Character.AllSkillsAgile;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches
{
    static class SkillSprintFixPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.EntityStates.Bandit2.Weapon.BaseSidearmState.FixedUpdate += fixStateSprintCancel;

            MethodInfo acridBiteAllowExitFire = AccessTools.DeclaredPropertyGetter(typeof(EntityStates.Croco.Bite), nameof(EntityStates.Croco.Bite.allowExitFire));
            if (acridBiteAllowExitFire != null)
            {
                new ILHook(acridBiteAllowExitFire, fixStateSprintCancel);
            }
            else
            {
                Log.Warning("Failed to find method EntityStates.Croco.Bite.get_allowExitFire");
            }

            MethodInfo acridSlashAllowExitFire = AccessTools.DeclaredPropertyGetter(typeof(EntityStates.Croco.Slash), nameof(EntityStates.Croco.Slash.allowExitFire));
            if (acridSlashAllowExitFire != null)
            {
                new ILHook(acridSlashAllowExitFire, fixStateSprintCancel);
            }
            else
            {
                Log.Warning("Failed to find method EntityStates.Croco.Slash.get_allowExitFire");
            }

            IL.EntityStates.Railgunner.Scope.BaseActive.FixedUpdate += fixStateSprintCancel;

            IL.EntityStates.Toolbot.FireNailgun.FixedUpdate += fixStateSprintCancel;
            IL.EntityStates.Toolbot.ToolbotDualWieldBase.FixedUpdate += fixStateSprintCancel;

            IL.EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam.FixedUpdate += fixStateSprintCancel;
        }

        static void fixStateSprintCancel(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int patchCount = 0;

            while (c.TryGotoNext(MoveType.After,
                                 x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(CharacterBody), nameof(CharacterBody.isSprinting)))))
            {
                c.EmitDelegate(shouldConsiderSprining);
                static bool shouldConsiderSprining(bool isSprinting)
                {
                    if (OverrideSkillsAgile.IsAllSkillsAgile)
                        return false;

                    return isSprinting;
                }

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error($"{il.Method.FullName}: Found 0 patch locations");
            }
            else
            {
                Log.Debug($"{il.Method.FullName}: Found {patchCount} patch location(s)");
            }
        }
    }
}
