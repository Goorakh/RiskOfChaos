using HG;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
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
        static Material _hurtBoxSniperTargetMaterial;
        static Material _hurtBoxBullseyeMaterial;
        static Material _colliderDefaultMaterial;
        
        [SystemInitializer]
        static void Init()
        {
            static Material createColorMaterial(Color color)
            {
                Shader standardShader = LegacyShaderAPI.Find("Shaders/Deferred/HGStandard");

                Material material;
                if (standardShader)
                {
                    material = new Material(standardShader);
                }
                else
                {
                    Log.Warning("Failed to find HGStandard shader");
                    material = new Material(Material.GetDefaultMaterial());
                }

                material.color = color;
                return material;
            }

            _hurtBoxSniperTargetMaterial = createColorMaterial(new Color32(224, 56, 44, 255));
            _hurtBoxBullseyeMaterial = createColorMaterial(new Color32(239, 222, 33, 255));
            _colliderDefaultMaterial = createColorMaterial(new Color32(169, 246, 252, 255));
        }

        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (NetworkClient.active)
            {
                InstanceTracker.GetInstancesList<CharacterModel>().TryDo(setupModel);

                CharacterModelHooks.OnCharacterModelStartGlobal += onCharacterModelStartGlobal;
            }
        }

        void OnDestroy()
        {
            CharacterModelHooks.OnCharacterModelStartGlobal -= onCharacterModelStartGlobal;
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
            rendererData.OwnerEffect = _effectComponent;
        }

        sealed class ColliderRendererController : MonoBehaviour
        {
            GameObject[] _colliderVisualObjects = [];
            GameObject[] _disabledModelObjects = [];
            CharacterModel.RendererInfo[] _detachedRendererInfos = [];

            CharacterModel _characterModel;
            ModelSkinController _modelSkinController;

            public ChaosEffectComponent OwnerEffect;

            void Awake()
            {
                _characterModel = GetComponent<CharacterModel>();
                if (!_characterModel)
                {
                    Log.Error("Cannot setup for character model with no CharacterModel instance");
                    enabled = false;
                }

                _modelSkinController = GetComponent<ModelSkinController>();
                if (_modelSkinController)
                {
                    _modelSkinController.onSkinApplied += onSkinApplied;
                }
            }

            void Start()
            {
                createColliderVisuals();

                if (OwnerEffect)
                {
                    OwnerEffect.OnEffectEnd += onOwnerEffectEnd;
                }
            }

            void OnDestroy()
            {
                if (OwnerEffect)
                {
                    OwnerEffect.OnEffectEnd -= onOwnerEffectEnd;
                }

                if (_modelSkinController)
                {
                    _modelSkinController.onSkinApplied -= onSkinApplied;
                }

                if (_characterModel)
                {
                    _characterModel.baseRendererInfos = _detachedRendererInfos;
                    foreach (CharacterModel.RendererInfo rendererInfo in _characterModel.baseRendererInfos)
                    {
                        if (rendererInfo.renderer)
                        {
                            rendererInfo.renderer.forceRenderingOff = false;
                        }
                    }

                    _detachedRendererInfos = [];

                    _characterModel.forceUpdate = true;
                }

                if (_disabledModelObjects.Length > 0)
                {
                    foreach (GameObject disabledModelObject in _disabledModelObjects)
                    {
                        if (disabledModelObject)
                        {
                            disabledModelObject.SetActive(true);
                        }
                    }

                    _disabledModelObjects = [];
                }

                cleanUpColliderVisuals();
            }

            void onOwnerEffectEnd(ChaosEffectComponent effectComponent)
            {
                Destroy(this);
            }

            void onSkinApplied(int localSkinIndex)
            {
                cleanUpColliderVisuals();
                createColliderVisuals();
            }

            static GameObject createVisualForCollider(Collider collider, out Vector3 visualLocalPosition, out Quaternion visualLocalRotation, out Vector3 visualLocalScale, out bool requireUniformScale)
            {
                Mesh visualMesh;
                switch (collider)
                {
                    case BoxCollider boxCollider:
                        visualLocalPosition = boxCollider.center;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = boxCollider.size;
                        requireUniformScale = false;
                        visualMesh = MeshUtils.GetPrimitiveMesh(PrimitiveType.Cube);

                        break;
                    case SphereCollider sphereCollider:
                        visualLocalPosition = sphereCollider.center;
                        visualLocalRotation = Quaternion.identity;
                        float diameter = sphereCollider.radius * 2f;
                        visualLocalScale = new Vector3(diameter, diameter, diameter);
                        requireUniformScale = true;
                        visualMesh = MeshUtils.GetPrimitiveMesh(PrimitiveType.Sphere);

                        break;
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
                                Log.Error($"Unhandled capsule direction: {capsuleCollider.direction}");
                                pointDirection = Vector3.up;
                                break;
                        }

                        visualLocalRotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, pointDirection);

                        float radius = capsuleCollider.radius;

                        // .height includes height of both half-spheres
                        float height = capsuleCollider.height - (radius * 2f);

                        if (radius > 0f && height >= 0f)
                        {
                            float normalizedRadius = 1f;
                            float normalizedHeight = height / radius;

                            visualMesh = MeshUtils.GetCapsuleMesh(normalizedRadius, normalizedHeight);
                        }
                        else
                        {
                            Log.Warning($"Invalid capsule collider (r={radius}, h={height}) on {Util.GetGameObjectHierarchyName(collider.gameObject)} ({collider.GetType().FullName})");
                            visualMesh = null;
                        }

                        visualLocalScale = new Vector3(radius, radius, radius);
                        requireUniformScale = false;

                        break;
                    case MeshCollider meshCollider:
                        visualLocalPosition = Vector3.zero;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one;
                        requireUniformScale = false;
                        visualMesh = meshCollider.sharedMesh;

                        break;
                    default:
                        Log.Warning($"Unhandled hurtbox collider type {collider?.GetType()}");

                        visualLocalPosition = Vector3.zero;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one;
                        requireUniformScale = false;
                        visualMesh = null;

                        break;
                }

                if (!visualMesh)
                {
                    Log.Error($"Failed to determine visual mesh for collider {collider}");
                    return null;
                }

                GameObject visualObject = new GameObject("ColliderVisual", [typeof(MeshFilter), typeof(MeshRenderer)]);

                MeshFilter visualMeshFilter = visualObject.GetComponent<MeshFilter>();
                visualMeshFilter.sharedMesh = visualMesh;

                return visualObject;
            }

            void cleanUpColliderVisuals()
            {
                if (_colliderVisualObjects.Length > 0)
                {
                    foreach (GameObject colliderVisual in _colliderVisualObjects)
                    {
                        if (colliderVisual)
                        {
                            Destroy(colliderVisual);
                        }
                    }

                    _colliderVisualObjects = [];
                }
            }

            void createColliderVisuals()
            {
                _colliderVisualObjects = [];
                _detachedRendererInfos = [];
                _disabledModelObjects = [];

                foreach (CharacterModel.RendererInfo rendererInfo in _characterModel.baseRendererInfos)
                {
                    if (rendererInfo.renderer)
                    {
                        rendererInfo.renderer.enabled = false;
                        rendererInfo.renderer.forceRenderingOff = true;
                    }
                }

                if (_characterModel.baseRendererInfos.Length > 0)
                {
                    _detachedRendererInfos = ArrayUtils.Clone(_characterModel.baseRendererInfos);
                }

                List<CharacterModel.RendererInfo> rendererInfos = [];
                List<GameObject> colliderVisualObjects = [];

                // If this isn't changed, animations won't play for some models while invisible
                if (_characterModel.TryGetComponent(out Animator animator))
                {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                List<Collider> validColliders = [];

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
                        validColliders.Add(collider);
                    }
                }

                if (validColliders.Count == 0)
                {
                    foreach (Collider collider in _characterModel.GetComponentsInChildren<Collider>(true))
                    {
                        if (collider.enabled && !collider.isTrigger)
                        {
                            validColliders.Add(collider);
                        }
                    }
                }

                foreach (Collider collider in validColliders)
                {
                    HurtBox hurtBox = collider.GetComponent<HurtBox>();

                    GameObject colliderVisual = createVisualForCollider(collider,
                                                                       out Vector3 visualLocalPosition,
                                                                       out Quaternion visualLocalRotation,
                                                                       out Vector3 visualLocalScale,
                                                                       out bool requireUniformScale);

                    if (!colliderVisual)
                        continue;

                    Transform visualTransform = colliderVisual.transform;
                    Transform colliderTransform = collider.transform;

                    Vector3 colliderPosition = colliderTransform.localPosition;
                    Quaternion colliderRotation = colliderTransform.localRotation;
                    Vector3 colliderScale = colliderTransform.localScale;

                    if (requireUniformScale)
                    {
                        float maxComponent = colliderScale.ComponentMax();
                        colliderScale = new Vector3(maxComponent, maxComponent, maxComponent);
                    }

                    visualTransform.SetParent(colliderTransform.parent);
                    visualTransform.localPosition = colliderPosition + visualLocalPosition;
                    visualTransform.localRotation = colliderRotation * visualLocalRotation;
                    visualTransform.localScale = Vector3.Scale(colliderScale, visualLocalScale);

                    Material material = _colliderDefaultMaterial;
                    if (hurtBox)
                    {
                        if (hurtBox.isSniperTarget)
                        {
                            material = _hurtBoxSniperTargetMaterial;
                        }
                        else if (hurtBox.isBullseye)
                        {
                            material = _hurtBoxBullseyeMaterial;
                        }
                    }

                    Renderer renderer = colliderVisual.GetComponent<Renderer>();
                    renderer.sharedMaterial = material;

                    rendererInfos.Add(new CharacterModel.RendererInfo
                    {
                        renderer = renderer,
                        ignoreOverlays = false,
                        hideOnDeath = false,
                        defaultShadowCastingMode = renderer.shadowCastingMode,
                        defaultMaterial = material
                    });

                    colliderVisualObjects.Add(colliderVisual);
                }

                _characterModel.baseRendererInfos = [.. rendererInfos];

                if (colliderVisualObjects.Count > 0)
                {
                    _colliderVisualObjects = [.. colliderVisualObjects];
                }

                List<GameObject> disabledModelObjects = [];

                foreach (Transform objectActivationTransform in _characterModel.gameObjectActivationTransforms)
                {
                    if (objectActivationTransform && objectActivationTransform.gameObject.activeSelf)
                    {
                        objectActivationTransform.gameObject.SetActive(false);
                        disabledModelObjects.Add(objectActivationTransform.gameObject);
                    }
                }

                foreach (Transform objectActivationTransform in _characterModel.customGameObjectActivationTransforms)
                {
                    if (objectActivationTransform && objectActivationTransform.gameObject.activeSelf)
                    {
                        objectActivationTransform.gameObject.SetActive(false);
                    }
                }

                if (disabledModelObjects.Count > 0)
                {
                    _disabledModelObjects = [.. disabledModelObjects];
                }

                _characterModel.forceUpdate = true;
            }
        }
    }
}
