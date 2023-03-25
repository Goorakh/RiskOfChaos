using HarmonyLib;
using HG;
using MonoMod.RuntimeDetour;
using RiskOfChaos.Networking;
using RoR2;
using RoR2.Skills;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Patches
{
    static class ForceLockPlayerSkillSlot
    {
        public const int SKILL_SLOT_COUNT = (int)SkillSlot.Special + 1;

        static readonly bool[] _lockedSlots = new bool[SKILL_SLOT_COUNT];

        static Sprite _lockedSkillIcon;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<CaptainSupplyDropSkillDef> loadPrepSupplyDropHandle = Addressables.LoadAssetAsync<CaptainSupplyDropSkillDef>("RoR2/Base/Captain/PrepSupplyDrop.asset");
            loadPrepSupplyDropHandle.Completed += handle =>
            {
                _lockedSkillIcon = handle.Result.exhaustedIcon;
            };
        }

        public static bool IsSkillSlotLocked(SkillSlot skillSlot)
        {
            return ArrayUtils.GetSafe(_lockedSlots, (int)skillSlot);
        }

        public static void SetSkillSlotLocked(SkillSlot skillSlot)
        {
            if (!ArrayUtils.IsInBounds(_lockedSlots, (int)skillSlot))
            {
                return;
            }

            _lockedSlots[(int)skillSlot] = true;
            tryApplyPatches();
        }

        static void resetLockedSlots()
        {
            ArrayUtils.SetAll(_lockedSlots, false);
        }

        static bool _hasAppliedPatches = false;

        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            if (!_lockedSlots.Any(l => l))
                return;

            On.RoR2.GenericSkill.CanExecute += GenericSkill_CanExecute;
            On.RoR2.GenericSkill.IsReady += GenericSkill_IsReady;
            new Hook(AccessTools.DeclaredPropertyGetter(typeof(GenericSkill), nameof(GenericSkill.icon)), GenericSkill_get_icon);

            Run.onRunDestroyGlobal += _ =>
            {
                resetLockedSlots();
            };

            StageCompleteMessage.OnReceive += _ =>
            {
                resetLockedSlots();
            };

            _hasAppliedPatches = true;
        }

        static bool isSkillLocked(GenericSkill skill)
        {
            CharacterBody body = skill.characterBody;
            if (!body)
                return false;

            return IsSkillSlotLocked(body.skillLocator.FindSkillSlot(skill));
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
