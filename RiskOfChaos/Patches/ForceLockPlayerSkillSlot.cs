using HarmonyLib;
using HG;
using MonoMod.RuntimeDetour;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.Networking;
using RoR2;
using RoR2.Skills;
using System;
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
            SyncLockedPlayerSkillSlots.OnReceive += SyncLockedPlayerSkillSlots_OnReceive;

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

            ref bool isLocked = ref _lockedSlots[(int)skillSlot];
            if (!isLocked)
            {
                isLocked = true;
                netSyncLockedSlots();
            }
        }

        static void netSyncLockedSlots()
        {
            new SyncLockedPlayerSkillSlots(_lockedSlots).Send(NetworkDestination.Server | NetworkDestination.Clients);
        }

        static void SyncLockedPlayerSkillSlots_OnReceive(bool[] lockedSkillSlots)
        {
            Array.Copy(lockedSkillSlots, _lockedSlots, SKILL_SLOT_COUNT);

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

            Stage.onServerStageComplete += _ =>
            {
                resetLockedSlots();
                netSyncLockedSlots();
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
