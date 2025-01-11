using RiskOfChaos.Collections;
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
        static Material _hurtBoxSniperTargetMaterial;
        static Material _hurtBoxBullseyeMaterial;
        static Material _hurtBoxDefaultMaterial;
        
        static Mesh _cubeMesh;
        static Mesh _sphereMesh;
        static Mesh _capsuleMesh;

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
            _hurtBoxDefaultMaterial = createColorMaterial(new Color32(169, 246, 252, 255));

            static Mesh getPrimitiveMesh(PrimitiveType primitiveType)
            {
                GameObject primitiveObject = GameObject.CreatePrimitive(primitiveType);
                Mesh mesh = null;
                if (primitiveObject && primitiveObject.TryGetComponent(out MeshFilter meshFilter))
                {
                    mesh = meshFilter.sharedMesh;
                    Destroy(primitiveObject);
                }

                if (!mesh)
                {
                    Log.Error($"Failed to find mesh for primitive type '{primitiveType}'");
                }

                return mesh;
            }

            _cubeMesh = getPrimitiveMesh(PrimitiveType.Cube);
            _sphereMesh = getPrimitiveMesh(PrimitiveType.Sphere);
            _capsuleMesh = getPrimitiveMesh(PrimitiveType.Capsule);
        }

        class ColliderRendererController : MonoBehaviour
        {
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
                Mesh visualMesh = null;
                switch (collider)
                {
                    case BoxCollider boxCollider:
                        visualLocalPosition = boxCollider.center;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = boxCollider.size;
                        visualMesh = _cubeMesh;

                        break;
                    case SphereCollider sphereCollider:
                        visualLocalPosition = sphereCollider.center;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one * (sphereCollider.radius * 2f);
                        visualMesh = _sphereMesh;

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
                                Log.Warning($"Unhandled capsule direction: {capsuleCollider.direction}");
                                pointDirection = Vector3.up;
                                break;
                        }
                        
                        visualLocalRotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, pointDirection);
                        
                        float diameter = capsuleCollider.radius * 2f;

                        visualLocalScale = new Vector3(diameter, capsuleCollider.height - diameter, diameter);

                        // TODO: Compute the actual collision shape mesh instead of stretching the default capsule
                        visualMesh = _capsuleMesh;

                        break;
                    case MeshCollider meshCollider:
                        visualLocalPosition = Vector3.zero;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one;
                        visualMesh = meshCollider.sharedMesh;

                        break;
                    default:
                        Log.Warning($"Unhandled hurtbox collider type {collider?.GetType()}");

                        visualLocalPosition = Vector3.zero;
                        visualLocalRotation = Quaternion.identity;
                        visualLocalScale = Vector3.one;
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

                        Material material = _hurtBoxDefaultMaterial;
                        if (hurtBox.isSniperTarget)
                        {
                            material = _hurtBoxSniperTargetMaterial;
                        }
                        else if (hurtBox.isBullseye)
                        {
                            material = _hurtBoxBullseyeMaterial;
                        }

                        Renderer renderer = hurtBoxVisual.GetComponent<Renderer>();
                        renderer.sharedMaterial = material;

                        _visualObjectRenderers.Add(renderer);

                        additionalRendererInfos.Add(new CharacterModel.RendererInfo
                        {
                            renderer = renderer,
                            ignoreOverlays = false,
                            hideOnDeath = false,
                            defaultShadowCastingMode = renderer.shadowCastingMode,
                            defaultMaterial = material
                        });

                        _visualObjects.Add(hurtBoxVisual);
                    }
                }

                _characterModel.baseRendererInfos = [.. _characterModel.baseRendererInfos, .. additionalRendererInfos];
            }
        }

        readonly ClearingObjectList<ColliderRendererController> _colliderRenderers = [];

        void Start()
        {
            if (NetworkClient.active)
            {
                List<CharacterModel> characterModels = InstanceTracker.GetInstancesList<CharacterModel>();

                _colliderRenderers.EnsureCapacity(characterModels.Count);

                characterModels.TryDo(setupModel);

                CharacterModelHooks.OnCharacterModelStartGlobal += onCharacterModelStartGlobal;
            }
        }

        void OnDestroy()
        {
            CharacterModelHooks.OnCharacterModelStartGlobal -= onCharacterModelStartGlobal;

            _colliderRenderers.ClearAndDispose(true);
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
        }
    }
}
