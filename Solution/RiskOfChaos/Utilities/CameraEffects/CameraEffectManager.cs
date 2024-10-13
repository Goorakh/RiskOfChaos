using RiskOfChaos.OLD_ModifierController;
using RiskOfChaos.ScreenEffect;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.Utilities.CameraEffects
{
    [Obsolete]
    public static class CameraEffectManager
    {
        static readonly List<CameraEffect> _activeEffects = [];
        public static readonly ReadOnlyCollection<CameraEffect> ActiveEffects = new ReadOnlyCollection<CameraEffect>(_activeEffects);

        static bool _updateEventEnabled;
        static bool updateEventEnabled
        {
            get
            {
                return _updateEventEnabled;
            }
            set
            {
                if (_updateEventEnabled == value)
                    return;

                _updateEventEnabled = value;

                if (_updateEventEnabled)
                {
                    RoR2Application.onUpdate += update;

#if DEBUG
                    Log.Debug("Update event enabled");
#endif
                }
                else
                {
                    RoR2Application.onUpdate -= update;

#if DEBUG
                    Log.Debug("Update event disabled");
#endif
                }
            }
        }

        public static CameraEffect AddEffect(Material effectMaterial, ScreenEffectType effectType)
        {
            return AddEffect(effectMaterial, effectType, null, ValueInterpolationFunctionType.Snap, 0f);
        }

        public static CameraEffect AddEffect(Material effectMaterial, ScreenEffectType effectType, MaterialPropertyInterpolator propertyInterpolator, ValueInterpolationFunctionType blendType, float interpolationTime)
        {
            foreach (CameraEffect existingCameraEffect in _activeEffects)
            {
                if (existingCameraEffect.Material == effectMaterial)
                {
                    Log.Warning($"Duplicate material effect instance registered: {effectMaterial}");
                    break;
                }
            }

            CameraEffect cameraEffect = new CameraEffect(effectMaterial, propertyInterpolator, effectType);
            _activeEffects.Add(cameraEffect);

            if (interpolationTime > 0f)
            {
                cameraEffect.StartInterpolatingIn(blendType, interpolationTime);
            }

            updateEventEnabled = true;

            return cameraEffect;
        }

        public static void RemoveEffect(Material effectMaterial, bool destroyMaterial = true)
        {
            RemoveEffect(effectMaterial, ValueInterpolationFunctionType.Snap, 0f, destroyMaterial);
        }

        public static InterpolationState RemoveEffect(Material effectMaterial, ValueInterpolationFunctionType blendType, float interpolationTime, bool destroyMaterial = true)
        {
            InterpolationState result = null;

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

                        result ??= cameraEffect.InterpolationState;
                    }
                    else
                    {
                        _activeEffects.RemoveAt(i);

                        if (destroyMaterial)
                        {
                            GameObject.Destroy(cameraEffect.Material);
                        }

                        result ??= cameraEffect.InterpolationState;
                    }
                }
            }

            return result;
        }

        static void update()
        {
            if (_activeEffects.Count == 0)
            {
                updateEventEnabled = false;
                return;
            }

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
                else if (cameraEffect.InterpolationState.IsFinished) // Interpolation has finished
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
                On.RoR2.UICamera.Awake += UICamera_Awake;
            }

            static void SceneCamera_Awake(On.RoR2.SceneCamera.orig_Awake orig, SceneCamera self)
            {
                orig(self);

                CameraEffectApplier cameraEffectApplier = self.camera.gameObject.AddComponent<CameraEffectApplier>();
                cameraEffectApplier._acceptEffectType = ScreenEffectType.World;
            }

            static void UICamera_Awake(On.RoR2.UICamera.orig_Awake orig, UICamera self)
            {
                orig(self);

                CameraEffectApplier cameraEffectApplier = self.camera.gameObject.AddComponent<CameraEffectApplier>();
                cameraEffectApplier._acceptEffectType = ScreenEffectType.UIAndWorld;
            }

            ScreenEffectType _acceptEffectType;

            void OnRenderImage(RenderTexture source, RenderTexture destination)
            {
                int activeEffectsCount = _activeEffects.Count;
                if (activeEffectsCount == 0)
                {
                    Graphics.Blit(source, destination);
                }
                else
                {
                    int effectsToApplyCount = 0;
                    CameraEffect[] effectsToApply = new CameraEffect[activeEffectsCount];

                    foreach (CameraEffect cameraEffect in _activeEffects)
                    {
                        if (cameraEffect.Type != _acceptEffectType)
                            continue;

                        effectsToApply[effectsToApplyCount++] = cameraEffect;
                    }

                    if (effectsToApplyCount > 0)
                    {
                        RenderTextureDescriptor temporaryTextureDescriptor = (destination ? destination : source).descriptor;

                        RenderTexture current = source;
                        for (int i = 0; i < effectsToApplyCount; i++)
                        {
                            RenderTexture tmp = i == effectsToApplyCount - 1 ? destination : RenderTexture.GetTemporary(temporaryTextureDescriptor);

                            Graphics.Blit(current, tmp, effectsToApply[i].Material);

                            if (i > 0)
                            {
                                RenderTexture.ReleaseTemporary(current);
                            }

                            current = tmp;
                        }
                    }
                    else
                    {
                        Graphics.Blit(source, destination);
                    }
                }
            }

            class BlitStep
            {
                public RenderTexture Destination;
                public Material Material;

                public BlitStep(RenderTexture destination, Material material)
                {
                    Destination = destination;
                    Material = material;
                }
            }
        }
    }
}
