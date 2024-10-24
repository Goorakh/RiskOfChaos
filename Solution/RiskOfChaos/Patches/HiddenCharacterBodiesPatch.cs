using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
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

            if (!_hiddenBodyIndices.Contains(bodyIndex))
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

            MethodDefinition invokeMethod = null;
            while (c.TryGotoNext(MoveType.Before, x => matchInvokeCharacterEvent(x, out invokeMethod)))
            {
                int invokeMethodParameterCount = invokeMethod.Parameters.Count + (!invokeMethod.IsStatic ? 1 : 0);

                ILLabel skipInvokeLabel = c.DefineLabel();

                emitGetIsHidden(c);
                c.Emit(OpCodes.Brtrue, skipInvokeLabel);

                c.Index++;

                ILLabel afterPatchLabel = c.DefineLabel();
                c.Emit(OpCodes.Br, afterPatchLabel);

                c.MarkLabel(skipInvokeLabel);

                for (int i = 0; i < invokeMethodParameterCount; i++)
                {
                    c.Emit(OpCodes.Pop);
                }

                c.MarkLabel(afterPatchLabel);

                c.SearchTarget = SearchTarget.Next;
            }
        }
    }
}
