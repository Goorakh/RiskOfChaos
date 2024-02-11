using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("render_colliders_only", 90f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class RenderCollidersOnly : TimedEffect
    {
        readonly List<GameObject> _hurtBoxVisualObjects = [];

        public override void OnStart()
        {
            InstanceTracker.GetInstancesList<CharacterModel>().TryDo(setupModel);

            On.RoR2.CharacterModel.Start += CharacterModel_Start;
        }

        public override void OnEnd()
        {
            On.RoR2.CharacterModel.Start -= CharacterModel_Start;

            InstanceTracker.GetInstancesList<CharacterModel>().TryDo(model =>
            {
                model.invisibilityCount--;
            });

            foreach (GameObject visualObject in _hurtBoxVisualObjects)
            {
                if (visualObject)
                {
                    GameObject.Destroy(visualObject);
                }
            }
        }

        void CharacterModel_Start(On.RoR2.CharacterModel.orig_Start orig, CharacterModel self)
        {
            orig(self);

            setupModel(self);
        }

        void setupModel(CharacterModel model)
        {
            model.invisibilityCount++;

            // If this isn't changed, animations won't play for some models while invisible
            if (model.TryGetComponent(out Animator animator))
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            HurtBox[] hurtBoxes;
            if (model.TryGetComponent(out HurtBoxGroup hurtBoxGroup))
            {
                hurtBoxes = hurtBoxGroup.hurtBoxes;
            }
            else
            {
                hurtBoxes = model.GetComponentsInChildren<HurtBox>();
            }

            foreach (HurtBox hurtBox in hurtBoxes)
            {
                foreach (Collider collider in hurtBox.GetComponents<Collider>())
                {
                    GameObject hurtBoxVisual;

                    Vector3 visualLocalPosition;
                    Quaternion visualLocalRotation;
                    Vector3 visualLocalScale;

                    switch (collider)
                    {
                        case BoxCollider boxCollider:
                            hurtBoxVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);

                            visualLocalPosition = boxCollider.center;
                            visualLocalRotation = Quaternion.identity;
                            visualLocalScale = boxCollider.size;
                            break;
                        case SphereCollider sphereCollider:
                            hurtBoxVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                            visualLocalPosition = sphereCollider.center;
                            visualLocalRotation = Quaternion.identity;
                            visualLocalScale = Vector3.one * (sphereCollider.radius * 2f);
                            break;
                        case CapsuleCollider capsuleCollider:
                            hurtBoxVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);

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
                            break;
                        case MeshCollider meshCollider:
                            hurtBoxVisual = new GameObject("Mesh");

                            visualLocalPosition = Vector3.zero;
                            visualLocalRotation = Quaternion.identity;
                            visualLocalScale = Vector3.one;

                            MeshFilter meshFilter = hurtBoxVisual.AddComponent<MeshFilter>();
                            meshFilter.sharedMesh = meshCollider.sharedMesh;

                            hurtBoxVisual.AddComponent<MeshRenderer>();

                            break;
                        default:
                            Log.Warning($"Unhandled hurtbox collider type {hurtBox.collider?.GetType()}");

                            hurtBoxVisual = null;

                            visualLocalPosition = Vector3.zero;
                            visualLocalRotation = Quaternion.identity;
                            visualLocalScale = Vector3.one;
                            break;
                    }

                    if (hurtBoxVisual)
                    {
                        Transform visualTransform = hurtBoxVisual.transform;
                        Transform hurtBoxTransform = hurtBox.transform;

                        visualTransform.SetParent(hurtBoxTransform.parent);
                        visualTransform.localPosition = hurtBoxTransform.localPosition + visualLocalPosition;
                        visualTransform.localRotation = hurtBoxTransform.localRotation * visualLocalRotation;
                        visualTransform.localScale = Vector3.Scale(hurtBoxTransform.localScale, visualLocalScale);

                        if (hurtBoxVisual.TryGetComponent(out Collider visualCollider))
                        {
                            GameObject.Destroy(visualCollider);
                        }

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

                        _hurtBoxVisualObjects.Add(hurtBoxVisual);
                    }
                }
            }
        }
    }
}
