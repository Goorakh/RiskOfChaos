using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Components;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class PickupControllerInheritDropletVelocityPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.PickupDropletController.CreatePickup += PickupDropletController_CreatePickup;
            IL.RoR2.PickupDropletController.CreateCommandCube += PickupDropletController_CreateCommandCube;
        }

        static void PickupDropletController_CreatePickup(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => GenericPickupController.CreatePickup(default)))))
            {
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(pickupInheritVelocity);

                static void pickupInheritVelocity(GenericPickupController pickupController, PickupDropletController pickupDropletController)
                {
                    if (pickupController)
                    {
                        inheritVelocity(pickupDropletController, pickupController.gameObject);
                    }
                }
            }
        }

        static void PickupDropletController_CreateCommandCube(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => NetworkServer.Spawn(default)))))
            {
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(commandCubeInheritVelocity);

                static void commandCubeInheritVelocity(GameObject commandCubeObj, PickupDropletController pickupDropletController)
                {
                    inheritVelocity(pickupDropletController, commandCubeObj);
                }
            }
        }

        static void inheritVelocity(PickupDropletController pickupDropletController, GameObject pickupObject)
        {
            if (!pickupDropletController || !pickupObject)
                return;

            if (pickupDropletController.GetComponent<AttractToPlayers>())
            {
                if (pickupDropletController.TryGetComponent(out Rigidbody dropletRigidbody) && pickupObject.TryGetComponent(out Rigidbody pickupRigidbody))
                {
                    pickupRigidbody.velocity += dropletRigidbody.velocity;
                }
            }
        }
    }
}
