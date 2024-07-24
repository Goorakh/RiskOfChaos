using HarmonyLib;
using LeTai.Asset.TranslucentImage;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using RiskOfChaos.Utilities.CameraEffects;
using RoR2;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ScreenBlurEffectApplier
    {
        [SystemInitializer]
        static void Init()
        {
            MethodInfo from = SymbolExtensions.GetMethodInfo<TranslucentImageSource>(_ => _.ProgressiveBlur(default));

            new ILHook(from, TranslucentImageSource_ProgressiveBlur);
        }

        static void TranslucentImageSource_ProgressiveBlur(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // Loop around to last instruction
            c.Index--;

            if (c.TryGotoPrev(x => x.MatchCall(SymbolExtensions.GetMethodInfo(() => Graphics.Blit(default, default, default(Material), default)))))
            {
                int temporaryTextureLocalIndex = -1;
                if (c.TryGotoPrev(MoveType.After,
                                  x => x.MatchLdloc(out temporaryTextureLocalIndex) && il.Body.Variables[temporaryTextureLocalIndex].VariableType.Is(typeof(RenderTexture))))
                {
                    c.EmitDelegate(applyCameraEffects);
                    static RenderTexture applyCameraEffects(RenderTexture texture)
                    {
                        int effectCount = CameraEffectManager.ActiveEffects.Count;
                        if (effectCount > 0)
                        {
                            RenderTextureDescriptor textureDescriptor = texture.descriptor;

                            foreach (CameraEffect cameraEffect in CameraEffectManager.ActiveEffects)
                            {
                                RenderTexture nextTexture = RenderTexture.GetTemporary(textureDescriptor);

                                Graphics.Blit(texture, nextTexture, cameraEffect.Material);

                                RenderTexture.ReleaseTemporary(texture);
                                texture = nextTexture;
                            }
                        }

                        return texture;
                    }

                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Stloc, temporaryTextureLocalIndex);
                }
                else
                {
                    Log.Error("Failed to find patch location");
                }
            }
            else
            {
                Log.Error("Failed to find Blit call");
            }
        }
    }
}
