using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("render_colliders_only", 90f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class RenderCollidersOnly : TimedEffect
    {
        class RendererData : MonoBehaviour
        {
            readonly List<GameObject> _visualObjects = [];
            readonly List<Renderer> _visualObjectRenderers = [];

            CharacterModel _characterModel;

            void Awake()
            {
                InstanceTracker.Add(this);
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);

                if (_characterModel)
                {
                    if (_visualObjectRenderers.Count > 0)
                    {
                        _characterModel.baseRendererInfos = _characterModel.baseRendererInfos.Where(ri => !_visualObjectRenderers.Contains(ri.renderer)).ToArray();
                    }

                    foreach (CharacterModel.RendererInfo rendererInfo in _characterModel.baseRendererInfos)
                    {
                        rendererInfo.renderer.forceRenderingOff = false;
                    }
                }

                foreach (GameObject visualObject in _visualObjects)
                {
                    if (visualObject)
                    {
                        Destroy(visualObject);
                    }
                }

                _visualObjects.Clear();
                _visualObjectRenderers.Clear();
            }

            static GameObject createVisualForCollider(Collider collider, out Vector3 visualLocalPosition, out Quaternion visualLocalRotation, out Vector3 visualLocalScale)
            {
                switch (collider)
                {
                    case BoxCollider boxCollider:
                        visualLocalPosition = boxCollider.center;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = boxCollider.size;

                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        if (cube.TryGetComponent(out Collider cubeObjectCollider))
                            Destroy(cubeObjectCollider);

                        return cube;
                    case SphereCollider sphereCollider:
                        visualLocalPosition = sphereCollider.center;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one * (sphereCollider.radius * 2f);

                        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        if (sphere.TryGetComponent(out Collider sphereObjectCollider))
                            Destroy(sphereObjectCollider);

                        return sphere;
                    case CapsuleCollider capsuleCollider:
                        visualLocalPosition = capsuleCollider.center;

                        Vector3 pointDirection;
                        switch (capsuleCollider.direction)
                        {
                            case 0: // X
                                pointDirection = Vector3.right;
                                break;
                            case 1: // Y
                                pointDirection = Vector3.up;
                                break;
                            case 2: // Z
                                pointDirection = Vector3.forward;
                                break;
                            default:
                                Log.Warning($"Unhandled capsule direction: {capsuleCollider.direction}");
                                pointDirection = Vector3.up;
                                break;
                        }

                        visualLocalRotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, pointDirection);

                        float diameter = capsuleCollider.radius * 2f;

                        // MeshFilter capsuleMeshFilter = hurtBoxVisual.GetComponent<MeshFilter>();
                        // capsuleMeshFilter.sharedMesh = getCapsuleMesh(capsuleCollider.height - diameter, capsuleCollider.radius, out Vector3 scale);

                        visualLocalScale = new Vector3(diameter, capsuleCollider.height - diameter, diameter);

                        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        if (capsule.TryGetComponent(out Collider capsuleObjectCollider))
                            Destroy(capsuleObjectCollider);

                        return capsule;
                    case MeshCollider meshCollider:
                        GameObject meshRendererObject = new GameObject("Mesh");

                        visualLocalPosition = Vector3.zero;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one;

                        MeshFilter meshFilter = meshRendererObject.AddComponent<MeshFilter>();
                        meshFilter.sharedMesh = meshCollider.sharedMesh;

                        meshRendererObject.AddComponent<MeshRenderer>();

                        return meshRendererObject;
                    default:
                        Log.Warning($"Unhandled hurtbox collider type {collider?.GetType()}");

                        visualLocalPosition = Vector3.zero;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one;

                        return null;
                }
            }

            public void SetupCharacterModel()
            {
                _characterModel = GetComponent<CharacterModel>();
                if (!_characterModel)
                {
                    Log.Error("Cannot setup for character model with no CharacterModel instance");
                    return;
                }

                foreach (CharacterModel.RendererInfo rendererInfo in _characterModel.baseRendererInfos)
                {
                    rendererInfo.renderer.forceRenderingOff = true;
                }

                List<CharacterModel.RendererInfo> additionalRendererInfos = [];

                // If this isn't changed, animations won't play for some models while invisible
                if (_characterModel.TryGetComponent(out Animator animator))
                {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                HurtBox[] hurtBoxes;
                if (_characterModel.TryGetComponent(out HurtBoxGroup hurtBoxGroup))
                {
                    hurtBoxes = hurtBoxGroup.hurtBoxes;
                }
                else
                {
                    hurtBoxes = _characterModel.GetComponentsInChildren<HurtBox>();
                }

                foreach (HurtBox hurtBox in hurtBoxes)
                {
                    foreach (Collider collider in hurtBox.GetComponents<Collider>())
                    {
                        GameObject hurtBoxVisual = createVisualForCollider(collider,
                                                                           out Vector3 visualLocalPosition,
                                                                           out Quaternion visualLocalRotation,
                                                                           out Vector3 visualLocalScale);

                        if (!hurtBoxVisual)
                            continue;
                        
                        Transform visualTransform = hurtBoxVisual.transform;
                        Transform hurtBoxTransform = hurtBox.transform;

                        visualTransform.SetParent(hurtBoxTransform.parent);
                        visualTransform.localPosition = hurtBoxTransform.localPosition + visualLocalPosition;
                        visualTransform.localRotation = hurtBoxTransform.localRotation * visualLocalRotation;
                        visualTransform.localScale = Vector3.Scale(hurtBoxTransform.localScale, visualLocalScale);

                        Color color;
                        if (hurtBox.isSniperTarget)
                        {
                            color = new Color(224f / 255f, 56f / 255f, 44f / 255f);
                        }
                        else if (hurtBox.isBullseye)
                        {
                            color = new Color(239f / 255f, 222f / 255f, 33f / 255f);
                        }
                        else
                        {
                            color = new Color(169f / 255f, 246f / 255f, 252f / 255f);
                        }

                        Renderer renderer = hurtBoxVisual.GetComponent<Renderer>();
                        renderer.material.color = color;

                        _visualObjectRenderers.Add(renderer);

                        additionalRendererInfos.Add(new CharacterModel.RendererInfo
                        {
                            renderer = renderer,
                            ignoreOverlays = false,
                            hideOnDeath = false,
                            defaultShadowCastingMode = renderer.shadowCastingMode,
                            defaultMaterial = renderer.sharedMaterial
                        });

                        _visualObjects.Add(hurtBoxVisual);
                    }
                }

                ArrayUtil.AppendRange(ref _characterModel.baseRendererInfos, additionalRendererInfos);
            }
        }

        public override void OnStart()
        {
            InstanceTracker.GetInstancesList<CharacterModel>().TryDo(setupModel);

            On.RoR2.CharacterModel.Start += CharacterModel_Start;
        }

        public override void OnEnd()
        {
            On.RoR2.CharacterModel.Start -= CharacterModel_Start;

            InstanceUtils.DestroyAllTrackedInstances<RendererData>();
        }

        void CharacterModel_Start(On.RoR2.CharacterModel.orig_Start orig, CharacterModel self)
        {
            orig(self);

            IEnumerator waitForModelInitThenSetupModel()
            {
                // Wait for skins to be applied first, since that overrides the model renderers
                yield return new WaitForEndOfFrame();

                // Edge case: effect may have ended while waiting
                if (TimeRemaining > 0f)
                {
                    setupModel(self);
                }
            }

            self.StartCoroutine(waitForModelInitThenSetupModel());
        }

        void setupModel(CharacterModel model)
        {
            RendererData rendererData = model.gameObject.AddComponent<RendererData>();
            rendererData.SetupCharacterModel();
        }
    }
}
