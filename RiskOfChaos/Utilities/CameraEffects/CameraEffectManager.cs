using RiskOfChaos.ModifierController;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.Utilities.CameraEffects
{
    public static class CameraEffectManager
    {
        static readonly List<CameraEffect> _activeEffects = [];

        public static readonly ReadOnlyCollection<CameraEffect> ActiveEffects;

        static CameraEffectManager()
        {
            ActiveEffects = new ReadOnlyCollection<CameraEffect>(_activeEffects);

            RoR2Application.onUpdate += update;
        }

        public static void AddEffect(Material effectMaterial)
        {
            AddEffect(effectMaterial, null, ValueInterpolationFunctionType.Snap, 0f);
        }

        public static InterpolationState AddEffect(Material effectMaterial, MaterialPropertyInterpolator propertyInterpolator, ValueInterpolationFunctionType blendType, float interpolationTime)
        {
            CameraEffect cameraEffect = new CameraEffect(effectMaterial, propertyInterpolator);
            _activeEffects.Add(cameraEffect);

            if (interpolationTime > 0f)
            {
                cameraEffect.StartInterpolatingIn(blendType, interpolationTime);
            }

            return cameraEffect.InterpolationState;
        }

        public static void RemoveEffect(Material effectMaterial, bool destroyMaterial = true)
        {
            RemoveEffect(effectMaterial, ValueInterpolationFunctionType.Snap, 0f, destroyMaterial);
        }

        public static InterpolationState RemoveEffect(Material effectMaterial, ValueInterpolationFunctionType blendType, float interpolationTime, bool destroyMaterial = true)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                CameraEffect cameraEffect = _activeEffects[i];
                if (cameraEffect.Material == effectMaterial)
                {
                    if (interpolationTime > 0f)
                    {
                        cameraEffect.StartInterpolatingOut(blendType, interpolationTime);

                        if (destroyMaterial)
                        {
                            cameraEffect.InterpolationState.OnFinish += () =>
                            {
                                GameObject.Destroy(cameraEffect.Material);
                            };
                        }

                        return cameraEffect.InterpolationState;
                    }
                    else
                    {
                        _activeEffects.RemoveAt(i);

                        if (destroyMaterial)
                        {
                            GameObject.Destroy(cameraEffect.Material);
                        }

                        return cameraEffect.InterpolationState;
                    }
                }
            }

            return null;
        }

        static void update()
        {
            if (_activeEffects.Count == 0)
                return;

            if (!Run.instance)
            {
                foreach (CameraEffect cameraEffect in _activeEffects)
                {
                    if (cameraEffect.InterpolationState.IsInterpolating)
                    {
                        cameraEffect.OnInterpolationFinished();
                    }
                }

                _activeEffects.Clear();

                return;
            }

            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                CameraEffect cameraEffect = _activeEffects[i];
                if (cameraEffect.InterpolationState.IsInterpolating)
                {
                    cameraEffect.OnInterpolationUpdate();
                }
                else if (cameraEffect.InterpolationDirection > ModificationProviderInterpolationDirection.None) // Interpolation has finished
                {
#if DEBUG
                    Log.Debug($"Camera effect {cameraEffect.Material} interpolation finished ({cameraEffect.InterpolationDirection})");
#endif

                    // If out interpolation finished, the modification is done and should be removed
                    if (cameraEffect.InterpolationDirection == ModificationProviderInterpolationDirection.Out)
                    {
                        _activeEffects.RemoveAt(i);
                    }

                    cameraEffect.OnInterpolationFinished();
                }
            }
        }

        class CameraEffectApplier : MonoBehaviour
        {
            [SystemInitializer]
            static void Init()
            {
                On.RoR2.SceneCamera.Awake += SceneCamera_Awake;
            }

            static void SceneCamera_Awake(On.RoR2.SceneCamera.orig_Awake orig, SceneCamera self)
            {
                orig(self);

                self.camera.gameObject.AddComponent<CameraEffectApplier>();
            }

            void OnRenderImage(RenderTexture source, RenderTexture destination)
            {
                int activeEffectsCount = _activeEffects.Count;
                if (activeEffectsCount == 0)
                {
                    Graphics.Blit(source, destination);
                }
                else
                {
                    RenderTextureDescriptor temporaryTextureDescriptor = (destination ? destination : source).descriptor;

                    RenderTexture lastTexture = source;
                    for (int i = 0; i < activeEffectsCount; i++)
                    {
                        RenderTexture nextTexture = i == activeEffectsCount - 1 ? destination : RenderTexture.GetTemporary(temporaryTextureDescriptor);

                        Graphics.Blit(lastTexture, nextTexture, _activeEffects[i].Material);

                        if (lastTexture != source)
                            RenderTexture.ReleaseTemporary(lastTexture);

                        lastTexture = nextTexture;
                    }
                }
            }
        }
    }
}
