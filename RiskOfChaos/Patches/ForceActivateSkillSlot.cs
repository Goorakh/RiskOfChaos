using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.SkillSlots;
using RoR2;
using System.Linq;

namespace RiskOfChaos.Patches
{
    static class ForceActivateSkillSlot
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.PlayerCharacterMasterController.FixedUpdate += hookPushInputState;
            IL.RoR2.CharacterAI.BaseAI.UpdateBodyInputs += hookPushInputState;
        }

        static bool tryGetSkillSlotIndex(string fieldName, out int slotIndex)
        {
            switch (fieldName)
            {
                case nameof(InputBankTest.skill1):
                    slotIndex = (int)SkillSlot.Primary;
                    return true;
                case nameof(InputBankTest.skill2):
                    slotIndex = (int)SkillSlot.Secondary;
                    return true;
                case nameof(InputBankTest.skill3):
                    slotIndex = (int)SkillSlot.Utility;
                    return true;
                case nameof(InputBankTest.skill4):
                    slotIndex = (int)SkillSlot.Special;
                    return true;
                default:
                    slotIndex = -1;
                    return false;
            }
        }

        static void hookPushInputState(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILCursor[] foundCursors;
            while (c.TryFindNext(out foundCursors,
                                 x => x.MatchLdflda(out FieldReference field) && field.DeclaringType.FullName == typeof(InputBankTest).FullName && field.Name.StartsWith("skill"),
                                 x => x.MatchCallOrCallvirt<InputBankTest.ButtonState>(nameof(InputBankTest.ButtonState.PushState))))
            {
                FieldReference buttonField = foundCursors[0].Next.Operand as FieldReference;
                if (tryGetSkillSlotIndex(buttonField.Name, out int slotIndex))
                {
                    ILCursor cursor = foundCursors[1];
                    cursor.Emit(OpCodes.Ldc_I4, slotIndex);
                    cursor.EmitDelegate((bool buttonState, int skillSlot) =>
                    {
                        if (SkillSlotModificationManager.Instance && SkillSlotModificationManager.Instance.IsSkillSlotForceActivated((SkillSlot)skillSlot))
                        {
                            // Don't actually have it active *all* the time, since some skills require the key to not be held
                            return RoR2Application.rng.nextNormalizedFloat > 0.2f;
                        }

                        return buttonState;
                    });
                }

                c.Index = foundCursors.Last().Index + 1;
            }
        }
    }
}
