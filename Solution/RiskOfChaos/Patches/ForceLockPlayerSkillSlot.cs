using HarmonyLib;
using MonoMod.RuntimeDetour;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Patches
{
    static class ForceLockPlayerSkillSlot
    {
        static Sprite _lockedSkillIcon;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<CaptainSupplyDropSkillDef> loadPrepSupplyDropHandle = Addressables.LoadAssetAsync<CaptainSupplyDropSkillDef>("RoR2/Base/Captain/PrepSupplyDrop.asset");
            loadPrepSupplyDropHandle.OnSuccess(s => _lockedSkillIcon = s.exhaustedIcon);

            On.RoR2.GenericSkill.CanExecute += GenericSkill_CanExecute;
            On.RoR2.GenericSkill.IsReady += GenericSkill_IsReady;
            new Hook(AccessTools.DeclaredPropertyGetter(typeof(GenericSkill), nameof(GenericSkill.icon)), GenericSkill_get_icon);

            SkillSlotModificationManager.OnSkillSlotUnlocked += SkillSlotModificationManager_OnSkillSlotUnlocked;
        }

        static bool isSkillLocked(GenericSkill skill)
        {
            if (!skill)
                return false;

            CharacterBody body = skill.characterBody;
            if (!body)
                return false;

            SkillSlotModificationManager skillSlotModificationManager = SkillSlotModificationManager.Instance;
            return skillSlotModificationManager && skillSlotModificationManager.LockedSlots.Contains(body.skillLocator.FindSkillSlot(skill));
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
