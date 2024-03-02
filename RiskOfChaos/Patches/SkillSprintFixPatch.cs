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

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            MethodInfo acridBiteAllowExitFire = AccessTools.DeclaredPropertyGetter(typeof(EntityStates.Croco.Bite), nameof(EntityStates.Croco.Bite.allowExitFire));
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            if (acridBiteAllowExitFire != null)
            {
                new ILHook(acridBiteAllowExitFire, fixStateSprintCancel);
            }
            else
            {
                Log.Warning("Failed to find method EntityStates.Croco.Bite.get_allowExitFire");
            }

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            MethodInfo acridSlashAllowExitFire = AccessTools.DeclaredPropertyGetter(typeof(EntityStates.Croco.Slash), nameof(EntityStates.Croco.Slash.allowExitFire));
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
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

        static bool forceAllSkillsAgileActive()
        {
            return OverrideSkillsAgile.IsAllSkillsAgile;
        }

        static void fixStateSprintCancel(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After,
                                 x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(CharacterBody), nameof(CharacterBody.isSprinting)))))
            {
                c.EmitDelegate((bool isSprinting) =>
                {
                    if (forceAllSkillsAgileActive())
                        return false;

                    return isSprinting;
                });
            }
        }
    }
}
