using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class HiddenCharacterBodiesPatch
    {
        static readonly List<BodyIndex> _hiddenBodyIndices = [];

        public static void HideBody(GameObject bodyPrefab)
        {
            BodyCatalog.availability.CallWhenAvailable(() =>
            {
                HideBody(BodyCatalog.FindBodyIndex(bodyPrefab));
            });
        }

        public static void HideBody(BodyIndex bodyIndex)
        {
            if (bodyIndex == BodyIndex.None)
                return;

            if (!IsHiddenBody(bodyIndex))
            {
                _hiddenBodyIndices.Add(bodyIndex);
                _hiddenBodyIndices.Sort();
            }
        }

        public static bool IsHiddenBody(BodyIndex bodyIndex)
        {
            return _hiddenBodyIndices.BinarySearch(bodyIndex) >= 0;
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterBody.OnEnable += CharacterBody_OnEnable;

            IL.RoR2.CharacterBody.Awake += removeInvokeEvents;
            IL.RoR2.CharacterBody.Start += removeInvokeEvents;
            IL.RoR2.CharacterBody.OnDestroy += removeInvokeEvents;
        }

        static void CharacterBody_OnEnable(On.RoR2.CharacterBody.orig_OnEnable orig, CharacterBody self)
        {
            orig(self);

            if (IsHiddenBody(self.bodyIndex))
            {
                CharacterBody.instancesList.Remove(self);
            }
        }

        static void removeInvokeEvents(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            void emitGetIsHidden(ILCursor c)
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(CharacterBody), nameof(CharacterBody.bodyIndex)));
                c.EmitDelegate(IsHiddenBody);
            }

            static bool matchInvokeCharacterEvent(Instruction x, out MethodDefinition invokeMethod)
            {
                invokeMethod = null;

                if (!x.MatchCallOrCallvirt(out MethodReference method))
                    return false;

                if (!string.Equals(method.Name, nameof(Action.Invoke)))
                    return false;

                TypeReference declaringTypeRef = method.DeclaringType;

                Type declaringType = declaringTypeRef.ResolveReflection();

                if (!typeof(Delegate).IsAssignableFrom(declaringType))
                    return false;

                invokeMethod = method.Resolve();
                return true;
            }

            int patchCount = 0;

            MethodDefinition invokeMethod = null;
            while (c.TryGotoNext(MoveType.Before, x => matchInvokeCharacterEvent(x, out invokeMethod)))
            {
                emitGetIsHidden(c);
                c.EmitSkipMethodCall(OpCodes.Brtrue, invokeMethod);

                patchCount++;

                c.SearchTarget = SearchTarget.Next;
            }

            if (patchCount == 0)
            {
                Log.Error($"{il.Method.FullName}: Failed to find any patch locations");
            }
            else
            {
                Log.Debug($"{il.Method.FullName}: Found {patchCount} patch location(s)");
            }
        }
    }
}
