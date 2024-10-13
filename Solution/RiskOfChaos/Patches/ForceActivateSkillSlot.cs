using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RiskOfChaos.OLD_ModifierController.SkillSlots;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.CharacterAI;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ForceActivateSkillSlot
    {
        static BodySkillPair[] _ignoreSkillSlots = [];

        [SystemInitializer(typeof(BodyCatalog))]
        static void InitIgnoreSkillSlots()
        {
            _ignoreSkillSlots = [
                // Beetle secondary is some unfinished ability that locks them in place forever, ignore it
                new BodySkillPair("BeetleBody", SkillSlot.Secondary)
            ];
        }

        static bool shouldIgnoreSkillSlot(BodyIndex bodyIndex, SkillSlot slot)
        {
            foreach (BodySkillPair ignoreSkillPair in _ignoreSkillSlots)
            {
                if (ignoreSkillPair.BodyIndex == bodyIndex && ignoreSkillPair.SkillSlot == slot)
                {
                    return true;
                }
            }

            return false;
        }

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.PlayerCharacterMasterController.PollButtonInput += hookPushInputState;
            IL.RoR2.CharacterAI.BaseAI.UpdateBodyInputs += hookPushInputState;
        }

        static bool tryGetSkillSlotIndex(string fieldName, out SkillSlot slotIndex)
        {
            switch (fieldName)
            {
                case nameof(InputBankTest.skill1):
                    slotIndex = SkillSlot.Primary;
                    return true;
                case nameof(InputBankTest.skill2):
                    slotIndex = SkillSlot.Secondary;
                    return true;
                case nameof(InputBankTest.skill3):
                    slotIndex = SkillSlot.Utility;
                    return true;
                case nameof(InputBankTest.skill4):
                    slotIndex = SkillSlot.Special;
                    return true;
                default:
                    slotIndex = SkillSlot.None;
                    return false;
            }
        }

        static void hookPushInputState(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int patchCount = 0;

            while (c.TryFindNext(out ILCursor[] foundCursors,
                                 x => x.MatchLdflda(out FieldReference field) && field.DeclaringType.Is(typeof(InputBankTest)) && field.Name.StartsWith("skill"),
                                 x => x.MatchCallOrCallvirt<InputBankTest.ButtonState>(nameof(InputBankTest.ButtonState.PushState))))
            {
                FieldReference buttonField = foundCursors[0].Next.Operand as FieldReference;
                if (tryGetSkillSlotIndex(buttonField.Name, out SkillSlot slotIndex))
                {
                    ILCursor cursor = foundCursors[1];
                    cursor.Emit(OpCodes.Ldc_I4, (int)slotIndex);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(overrideButtonState);
                    static bool overrideButtonState(bool origButtonState, SkillSlot skillSlot, MonoBehaviour instance)
                    {
                        if (SkillSlotModificationManager.Instance && SkillSlotModificationManager.Instance.IsSkillSlotForceActivated(skillSlot))
                        {
                            CharacterBody body;
                            switch (instance)
                            {
                                case PlayerCharacterMasterController playerCharacter:
                                    body = playerCharacter.body;
                                    break;
                                case BaseAI ai:
                                    body = ai.body;
                                    break;
                                default:
                                    Log.Error($"Unhandled instance type: {instance}");
                                    body = null;
                                    break;
                            }

                            if (body && !shouldIgnoreSkillSlot(body.bodyIndex, skillSlot))
                            {
                                // Don't actually have it active *all* the time, since some skills require the key to not be held
                                return RoR2Application.rng.nextNormalizedFloat > 0.2f;
                            }
                        }

                        return origButtonState;
                    }
                }

                c.Index = foundCursors.Last().Index + 1;

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error("Found 0 patch locations");
            }
#if DEBUG
            else
            {
                Log.Debug($"Found {patchCount} patch location(s)");
            }
#endif
        }
    }
}
