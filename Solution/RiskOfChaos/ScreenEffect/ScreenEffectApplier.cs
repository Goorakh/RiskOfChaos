using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ScreenEffect
{
    public sealed class ScreenEffectApplier : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.SceneCamera.Awake += SceneCamera_Awake;
            On.RoR2.UICamera.Awake += UICamera_Awake;
        }

        static void SceneCamera_Awake(On.RoR2.SceneCamera.orig_Awake orig, SceneCamera self)
        {
            orig(self);

            ScreenEffectApplier screenEffectApplier = self.camera.gameObject.AddComponent<ScreenEffectApplier>();
            screenEffectApplier._acceptEffectType = ScreenEffectType.World;
        }

        static void UICamera_Awake(On.RoR2.UICamera.orig_Awake orig, UICamera self)
        {
            orig(self);

            ScreenEffectApplier screenEffectApplier = self.camera.gameObject.AddComponent<ScreenEffectApplier>();
            screenEffectApplier._acceptEffectType = ScreenEffectType.UIAndWorld;
        }

        ScreenEffectType _acceptEffectType;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            IList<ScreenEffectComponent> screenEffects = null;
            if (ScreenEffectManager.Instance)
            {
                screenEffects = ScreenEffectManager.Instance.GetActiveScreenEffects(_acceptEffectType);
            }

            if (screenEffects == null || screenEffects.Count == 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            RenderTextureDescriptor temporaryTextureDescriptor = (destination ? destination : source).descriptor;

            RenderTexture current = source;
            for (int i = 0; i < screenEffects.Count; i++)
            {
                RenderTexture tmp = i == screenEffects.Count - 1 ? destination : RenderTexture.GetTemporary(temporaryTextureDescriptor);

                Graphics.Blit(current, tmp, screenEffects[i].ScreenEffectMaterial);

                if (i > 0)
                {
                    RenderTexture.ReleaseTemporary(current);
                }

                current = tmp;
            }
        }
    }
}
