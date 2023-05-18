using HarmonyLib;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.UI;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class PlayerRealTimePatch
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GenericSkill.Awake += GenericSkill_Awake;
            On.RoR2.GenericSkill.OnDestroy += GenericSkill_OnDestroy;

            On.RoR2.GenericSkill.CalculateFinalRechargeInterval += GenericSkill_CalculateFinalRechargeInterval;

            On.RoR2.UI.SkillIcon.Update += SkillIcon_Update;
            IL.RoR2.UI.EquipmentIcon.GenerateDisplayData += EquipmentIcon_GenerateDisplayData;
        }

        static void GenericSkill_Awake(On.RoR2.GenericSkill.orig_Awake orig, GenericSkill self)
        {
            orig(self);

            if (self.characterBody && self.characterBody.hasAuthority && TimeScaleModificationManager.Instance)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                TimeScaleModificationManager.Instance.OnValueModificationUpdated += self.RecalculateFinalRechargeInterval;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }

        static void GenericSkill_OnDestroy(On.RoR2.GenericSkill.orig_OnDestroy orig, GenericSkill self)
        {
            orig(self);

            if (self.characterBody && self.characterBody.hasAuthority && TimeScaleModificationManager.Instance)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                TimeScaleModificationManager.Instance.OnValueModificationUpdated -= self.RecalculateFinalRechargeInterval;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }

        static float GenericSkill_CalculateFinalRechargeInterval(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self)
        {
            float result;
            try
            {
                result = orig(self);
            }
            catch (Exception e)
            {
                Debug.LogError(e, self);
                return 0f;
            }

            if (self.characterBody && self.characterBody.isPlayerControlled && TimeScaleModificationManager.Instance)
            {
                return result * TimeScaleModificationManager.Instance.NetworkPlayerRealtimeTimeScaleMultiplier;
            }
            else
            {
                return result;
            }
        }

        // On hook instead of a proper IL hook because BetterUI hooks this same method and just overrides the cooldownText instead of using an IL hook
        static void SkillIcon_Update(On.RoR2.UI.SkillIcon.orig_Update orig, SkillIcon self)
        {
            orig(self);

            if (self.targetSkill && self.cooldownText && self.cooldownText.gameObject.activeSelf && TimeScaleModificationManager.Instance)
            {
                StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

                stringBuilder.AppendInt(Mathf.CeilToInt(self.targetSkill.cooldownRemaining / TimeScaleModificationManager.Instance.NetworkPlayerRealtimeTimeScaleMultiplier));

                self.cooldownText.SetText(stringBuilder);

                HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
            }
        }

        static void EquipmentIcon_GenerateDisplayData(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int chargeFinishTimeLocalIndex = -1;

            ILCursor[] foundCursors;
            if (c.TryFindNext(out foundCursors,
                              x => x.MatchLdfld<EquipmentState>(nameof(EquipmentState.chargeFinishTime)),
                              x => x.MatchStloc(out chargeFinishTimeLocalIndex)))
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                const string DISPLAY_DATA_COOLDOWN_VALUE_FIELD_NAME = nameof(EquipmentIcon.DisplayData.cooldownValue);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                c = foundCursors.Last();
                if (c.TryFindNext(out foundCursors,
                                  x => x.MatchLdloc(chargeFinishTimeLocalIndex) || x.MatchLdloca(chargeFinishTimeLocalIndex),
                                  x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Run.FixedTimeStamp), nameof(Run.FixedTimeStamp.timeUntilClamped))),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => Mathf.CeilToInt(default))),
                                  x => x.MatchStfld<EquipmentIcon.DisplayData>(DISPLAY_DATA_COOLDOWN_VALUE_FIELD_NAME)))
                {
                    ILCursor cursor = foundCursors[2];

                    cursor.EmitDelegate((float cooldown) =>
                    {
                        if (!TimeScaleModificationManager.Instance)
                            return cooldown;

                        return cooldown / TimeScaleModificationManager.Instance.NetworkPlayerRealtimeTimeScaleMultiplier;
                    });
                }
                else
                {
                    Log.Warning("Failed to find patch location");
                }
            }
            else
            {
                Log.Warning("Failed to find cooldown local index");
            }
        }
    }
}
