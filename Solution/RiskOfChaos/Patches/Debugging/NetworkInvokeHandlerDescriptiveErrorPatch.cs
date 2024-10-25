using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using System.Reflection;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches.Debugging
{
    // Adds actual error logging to NetworkConnection.InvokeDelegateWithCatch instead of a generic message
#if DEBUG
    static class NetworkInvokeHandlerDescriptiveErrorPatch
    {
        [SystemInitializer]
        static void Init()
        {
            MethodInfo targetMethod = SymbolExtensions.GetMethodInfo<NetworkConnection>(_ => _.InvokeDelegateWithCatch(default, default));
            if (targetMethod == null)
            {
                Log.Error("Failed to find InvokeDelegateWithCatch method");
                return;
            }

            new ILHook(targetMethod, NetworkConnection_InvokeDelegateWithCatch);
        }

        static void NetworkConnection_InvokeDelegateWithCatch(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int patchCount = 0;

            foreach (ExceptionHandler exceptionHandler in il.Method.Body.ExceptionHandlers)
            {
                if (exceptionHandler.HandlerType != ExceptionHandlerType.Catch)
                    continue;

                Instruction handlerStart = exceptionHandler.HandlerStart;
                c.Goto(handlerStart, MoveType.Before);

                c.Emit(OpCodes.Dup);
                handlerStart = c.Prev;

                c.EmitDelegate(Log.Error_NoCallerPrefix);

                if (exceptionHandler.TryEnd == exceptionHandler.HandlerStart)
                {
                    exceptionHandler.TryEnd = handlerStart;
                }

                exceptionHandler.HandlerStart = handlerStart;

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error("Found 0 patch locations");
            }
            else
            {
                Log.Debug($"Patched {patchCount} exception handler(s)");
            }
        }
    }
#endif
}
