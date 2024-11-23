using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("render_colliders_only", 90f, AllowDuplicates = false)]
    public sealed class RenderCollidersOnly : MonoBehaviour
    {
        class ColliderRendererController : MonoBehaviour
        {
            static readonly Color _hurtBoxSniperTargetColor = new Color(224f / 255f, 56f / 255f, 44f / 255f);
            static readonly Color _hurtBoxBullseyeColor = new Color(239f / 255f, 222f / 255f, 33f / 255f);
            static readonly Color _hurtBoxColor = new Color(169f / 255f, 246f / 255f, 252f / 255f);

            readonly List<GameObject> _visualObjects = [];
            readonly List<Renderer> _visualObjectRenderers = [];

            CharacterModel _characterModel;

            void Awake()
            {
                _characterModel = GetComponent<CharacterModel>();
                if (!_characterModel)
                {
                    Log.Error("Cannot setup for character model with no CharacterModel instance");
                    enabled = false;
                }
            }

            void Start()
            {
                setupCharacterModel();
            }

            void OnDestroy()
            {
                if (_characterModel)
                {
                    List<CharacterModel.RendererInfo> rendererInfos = [.. _characterModel.baseRendererInfos];

                    bool rendererInfosChanged = false;
                    foreach (Renderer visualRenderer in _visualObjectRenderers)
                    {
                        if (visualRenderer)
                        {
                            for (int i = rendererInfos.Count - 1; i >= 0; i--)
                            {
                                Renderer renderer = rendererInfos[i].renderer;
                                if (!renderer || renderer == visualRenderer)
                                {
                                    rendererInfos.RemoveAt(i);
                                    rendererInfosChanged = true;
                                }
                            }
                        }
                    }

                    if (rendererInfosChanged)
                    {
                        _characterModel.baseRendererInfos = [.. rendererInfos];
                    }

                    foreach (CharacterModel.RendererInfo rendererInfo in _characterModel.baseRendererInfos)
                    {
                        if (rendererInfo.renderer)
                        {
                            rendererInfo.renderer.forceRenderingOff = false;
                        }
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

            void setupCharacterModel()
            {
                foreach (CharacterModel.RendererInfo rendererInfo in _characterModel.baseRendererInfos)
                {
                    if (rendererInfo.renderer)
                    {
                        rendererInfo.renderer.forceRenderingOff = true;
                    }
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
                            color = _hurtBoxSniperTargetColor;
                        }
                        else if (hurtBox.isBullseye)
                        {
                            color = _hurtBoxBullseyeColor;
                        }
                        else
                        {
                            color = _hurtBoxColor;
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

                _characterModel.baseRendererInfos = [.. _characterModel.baseRendererInfos, .. additionalRendererInfos];
            }
        }

        readonly List<ColliderRendererController> _colliderRenderers = [];

        readonly List<OnDestroyCallback> _destroyCallbacks = [];

        bool _trackedObjectDestroyed;

        void Start()
        {
            if (NetworkClient.active)
            {
                List<CharacterModel> characterModels = InstanceTracker.GetInstancesList<CharacterModel>();

                _colliderRenderers.EnsureCapacity(characterModels.Count);
                _destroyCallbacks.EnsureCapacity(characterModels.Count);

                characterModels.TryDo(setupModel);

                CharacterModelHooks.OnCharacterModelStartGlobal += onCharacterModelStartGlobal;
            }
        }

        void OnDestroy()
        {
            CharacterModelHooks.OnCharacterModelStartGlobal -= onCharacterModelStartGlobal;

            foreach (OnDestroyCallback destroyCallback in _destroyCallbacks)
            {
                if (destroyCallback)
                {
                    OnDestroyCallback.RemoveCallback(destroyCallback);
                }
            }

            _destroyCallbacks.Clear();

            foreach (ColliderRendererController colliderRenderer in _colliderRenderers)
            {
                if (colliderRenderer)
                {
                    Destroy(colliderRenderer);
                }
            }

            _colliderRenderers.Clear();
        }

        void FixedUpdate()
        {
            if (_trackedObjectDestroyed)
            {
                _trackedObjectDestroyed = false;

                UnityObjectUtils.RemoveAllDestroyed(_destroyCallbacks);

                int destroyedColliderRenderers = UnityObjectUtils.RemoveAllDestroyed(_colliderRenderers);
                Log.Debug($"Cleared {destroyedColliderRenderers} destroyed collider renderer(s)");
            }
        }

        void onCharacterModelStartGlobal(CharacterModel model)
        {
            StartCoroutine(waitForModelInitThenSetupModel(model));
        }

        IEnumerator waitForModelInitThenSetupModel(CharacterModel model)
        {
            // Wait for skins to be applied first, since that overrides the model renderers
            yield return new WaitForEndOfFrame();

            setupModel(model);
        }

        void setupModel(CharacterModel model)
        {
            if (!NetworkClient.active)
                return;

            ColliderRendererController rendererData = model.gameObject.AddComponent<ColliderRendererController>();

            _colliderRenderers.Add(rendererData);

            OnDestroyCallback destroyCallback = OnDestroyCallback.AddCallback(rendererData.gameObject, _ =>
            {
                _trackedObjectDestroyed = true;
            });

            _destroyCallbacks.Add(destroyCallback);
        }
    }
}
