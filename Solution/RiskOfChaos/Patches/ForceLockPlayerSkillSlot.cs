using HarmonyLib;
using MonoMod.RuntimeDetour;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ForceLockPlayerSkillSlot
    {
        static Sprite _lockedSkillIcon;

        [SystemInitializer]
        static void Init()
        {
            AddressableUtil.LoadTempAssetAsync<Sprite>($"{AddressableGuids.RoR2_Base_UI_texGenericSkillIcons_png}[texGenericSkillIcons_0]").OnSuccess(s => _lockedSkillIcon = s);

            On.RoR2.GenericSkill.CanExecute += GenericSkill_CanExecute;
            On.RoR2.GenericSkill.IsReady += GenericSkill_IsReady;
            new Hook(AccessTools.DeclaredPropertyGetter(typeof(GenericSkill), nameof(GenericSkill.icon)), GenericSkill_get_icon);

            SkillSlotModificationManager.OnSkillSlotUnlocked += SkillSlotModificationManager_OnSkillSlotUnlocked;
        }

        static bool isSkillLocked(GenericSkill skill)
        {
            if (!skill)
                return false;

            SkillSlotModificationManager skillSlotModificationManager = SkillSlotModificationManager.Instance;
            if (!skillSlotModificationManager)
                return false;

            CharacterBody body = skill.characterBody;
            if (!body)
                return false;

            SkillLocator skillLocator = body.skillLocator;
            if (!skillLocator)
                return false;

            SkillSlot skillSlot = skillLocator.FindSkillSlot(skill);
            if (skillSlot == SkillSlot.None)
                return false;

            return skillSlotModificationManager.LockedSlots.Contains(skillSlot);
        }

        static bool GenericSkill_IsReady(On.RoR2.GenericSkill.orig_IsReady orig, GenericSkill self)
        {
            return orig(self) && !isSkillLocked(self);
        }

        static bool GenericSkill_CanExecute(On.RoR2.GenericSkill.orig_CanExecute orig, GenericSkill self)
        {
            return orig(self) && !isSkillLocked(self);
        }

        delegate Sprite GenericSkill_orig_get_icon(GenericSkill self);
        static Sprite GenericSkill_get_icon(GenericSkill_orig_get_icon orig, GenericSkill self)
        {
            Sprite icon = orig(self);

            if (isSkillLocked(self) && _lockedSkillIcon)
            {
                icon = _lockedSkillIcon;
            }

            return icon;
        }

        static void SkillSlotModificationManager_OnSkillSlotUnlocked(SkillSlot slot)
        {
            foreach (HUD hud in HUD.readOnlyInstanceList)
            {
                foreach (SkillIcon skillIcon in hud.skillIcons)
                {
                    if (skillIcon.targetSkillSlot == slot)
                    {
                        if (skillIcon.flashPanelObject)
                        {
                            skillIcon.flashPanelObject.SetActive(true);
                        }
                    }
                }
            }
        }
    }
}
