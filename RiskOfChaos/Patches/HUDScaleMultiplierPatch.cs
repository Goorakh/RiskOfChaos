using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RiskOfChaos.ModifierController.UI;
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
            On.RoR2.UI.HUDScaleController.Start += HUDScaleController_Start;
            IL.RoR2.UI.HUDScaleController.SetScale += HUDScaleController_SetScale;
        }

        static void HUDScaleController_Start(On.RoR2.UI.HUDScaleController.orig_Start orig, HUDScaleController self)
        {
            orig(self);

            HUDScaleMultiplierChangedListener multiplierChangedListener = self.gameObject.AddComponent<HUDScaleMultiplierChangedListener>();
            multiplierChangedListener.ScaleController = self;
        }

        static void HUDScaleController_SetScale(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchCall(AccessTools.DeclaredConstructor(typeof(Vector3), [typeof(float), typeof(float), typeof(float)]))))
            {
                int scaleLocalIndex = -1;
                if (c.Clone().TryGotoPrev(x => x.MatchLdloca(out scaleLocalIndex) && il.Method.Body.Variables[scaleLocalIndex].VariableType.Is(typeof(Vector3))))
                {
                    c.Emit(OpCodes.Ldloca, scaleLocalIndex);
                    c.EmitDelegate(modifyScale);
                    static void modifyScale(ref Vector3 scale)
                    {
                        if (UIModificationManager.Instance && UIModificationManager.Instance.AnyModificationActive)
                        {
                            scale *= UIModificationManager.Instance.HudScaleMultiplier;
                        }
                    }
                }
                else
                {
                    Log.Error("Failed to find scale local index");
                }
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }

        class HUDScaleMultiplierChangedListener : MonoBehaviour
        {
            public HUDScaleController ScaleController { get; set; }

            void OnEnable()
            {
                UIModificationManager.OnHudScaleMultiplierChanged += UIModificationManager_OnHudScaleMultiplierChanged;
            }

            void OnDisable()
            {
                UIModificationManager.OnHudScaleMultiplierChanged -= UIModificationManager_OnHudScaleMultiplierChanged;
            }

            void UIModificationManager_OnHudScaleMultiplierChanged(float newScaleMultiplier)
            {
                ScaleController.SetScale();
            }
        }
    }
}
