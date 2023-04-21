using HarmonyLib;
using HG;
using MonoMod.RuntimeDetour;
using RiskOfChaos.ModifierController.SkillSlots;
using RiskOfChaos.Networking;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using System.Linq;
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
            loadPrepSupplyDropHandle.Completed += handle =>
            {
                _lockedSkillIcon = handle.Result.exhaustedIcon;
            };

            On.RoR2.GenericSkill.CanExecute += GenericSkill_CanExecute;
            On.RoR2.GenericSkill.IsReady += GenericSkill_IsReady;
            new Hook(AccessTools.DeclaredPropertyGetter(typeof(GenericSkill), nameof(GenericSkill.icon)), GenericSkill_get_icon);
        }

        static bool isSkillLocked(GenericSkill skill)
        {
            if (!skill)
                return false;

            CharacterBody body = skill.characterBody;
            if (!body)
                return false;

            return SkillSlotModificationManager.Instance && SkillSlotModificationManager.Instance.IsSkillSlotLocked(body.skillLocator.FindSkillSlot(skill));
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
            Sprite defaultIcon = orig(self);

            if (isSkillLocked(self) && _lockedSkillIcon)
            {
                return _lockedSkillIcon;
            }

            return defaultIcon;
        }
    }
}
