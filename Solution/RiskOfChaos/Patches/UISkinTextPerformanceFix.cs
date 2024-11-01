using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using TMPro;

namespace RiskOfChaos.Patches
{
    static class UISkinTextPerformanceFix
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.UI.UISkinData.TextStyle.Apply += TextStyle_Apply;
        }

        static void TextStyle_Apply(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<TextMeshProUGUI>(out ParameterDefinition textLabelParameter))
            {
                Log.Error("failed to find text label parameter");
                return;
            }

            ILLabel afterSetFontSizeLabel = null;
            if (c.TryFindNext(out ILCursor[] cursors,
                              x => x.MatchLdfld<UISkinData.TextStyle>(nameof(UISkinData.TextStyle.fontSize)),
                              x => x.MatchBeq(out afterSetFontSizeLabel)))
            {
                ILCursor cursor = cursors[1];
                cursor.Index++;
                cursor.Emit(OpCodes.Ldarg, textLabelParameter);
                cursor.Emit(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(TMP_Text), nameof(TMP_Text.enableAutoSizing)));
                cursor.Emit(OpCodes.Brtrue, afterSetFontSizeLabel);
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }
    }
}
