using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.CharacterAI;
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

            SkillSlot slotIndex = SkillSlot.None;
            while (c.TryFindNext(out ILCursor[] foundCursors,
                                 x => x.MatchLdflda(out FieldReference field) && field.DeclaringType.Is(typeof(InputBankTest)) && tryGetSkillSlotIndex(field.Name, out slotIndex),
                                 x => x.MatchCallOrCallvirt<InputBankTest.ButtonState>(nameof(InputBankTest.ButtonState.PushState))))
            {
                ILCursor cursor = foundCursors[1];
                cursor.Emit(OpCodes.Ldc_I4, (int)slotIndex);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(overrideButtonState);
                static bool overrideButtonState(bool origButtonState, SkillSlot skillSlot, MonoBehaviour instance)
                {
                    SkillSlotModificationManager skillSlotModificationManager = SkillSlotModificationManager.Instance;
                    if (skillSlotModificationManager && skillSlotModificationManager.ForceActivatedSlots.Contains(skillSlot))
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

                c.Index = foundCursors[foundCursors.Length - 1].Index;
                c.SearchTarget = SearchTarget.Next;

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error("Found 0 patch locations");
            }
            else
            {
                Log.Debug($"Found {patchCount} patch location(s)");
            }
        }
    }
}
