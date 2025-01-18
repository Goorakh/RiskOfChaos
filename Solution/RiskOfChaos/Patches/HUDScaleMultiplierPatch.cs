using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModificationController.UI;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class HUDScaleMultiplierPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.UI.HUDScaleController.SetScale += HUDScaleController_SetScale;

            UIModificationManager.OnHudScaleMultiplierChanged += onHudScaleMultiplierChanged;
        }

        static float getHudScaleMultiplier()
        {
            if (!UIModificationManager.Instance)
                return 1f;

            return UIModificationManager.Instance.HudScaleMultiplier;
        }

        static void HUDScaleController_SetScale(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.Before,
                               x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertySetter(typeof(Transform), nameof(Transform.localScale)))))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            VariableDefinition transformVariable = il.AddVariable<Transform>();
            VariableDefinition scaleVariable = il.AddVariable<Vector3>();

            c.EmitStoreStack(transformVariable, scaleVariable);

            c.Emit(OpCodes.Ldloc, transformVariable);
            c.EmitDelegate(getScale);
            static Vector3 getScale(Vector3 scale, Transform transform)
            {
                return scale * getHudScaleMultiplier();
            }
        }

        static void onHudScaleMultiplierChanged(float newScaleMultiplier)
        {
            foreach (HUDScaleController hudScaleController in HUDScaleController.instancesList)
            {
                hudScaleController.SetScale();
            }
        }
    }
}
