using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Components;
using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    static class ProjectileBounceHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Projectile.ProjectileManager.InitializeProjectile += ProjectileManager_InitializeProjectile;

            IL.RoR2.Projectile.ProjectileController.OnCollisionEnter += ProjectileController_tryBouncePatch;
            IL.RoR2.Projectile.ProjectileControllerTrigger.OnTriggerEnter += ProjectileController_tryBouncePatch;

            IL.EntityStates.AimThrowableBase.FireProjectile += AimThrowableBase_FireProjectile;
        }

        static bool isBouncingEnabled => maxBounces > 0;

        static int maxBounces => ProjectileModificationManager.Instance ? ProjectileModificationManager.Instance.ProjectileBounceCount : 0;

        static void ProjectileManager_InitializeProjectile(On.RoR2.Projectile.ProjectileManager.orig_InitializeProjectile orig, ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
        {
            orig(projectileController, fireProjectileInfo);

            if (isBouncingEnabled)
            {
                projectileController.gameObject.AddComponent<ProjectileEnvironmentBounceBehavior>();
            }
        }

        static void ProjectileController_tryBouncePatch(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int impactInfoLocalIndex = -1;
            if (c.TryFindNext(out ILCursor[] foundCursors,
                              x => x.MatchInitobj<ProjectileImpactInfo>(),
                              x => x.MatchStloc(out impactInfoLocalIndex)))
            {
                ILCursor cursor = foundCursors[1];
                cursor.Index++;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloca, impactInfoLocalIndex);
                cursor.EmitDelegate(tryBounce);
                static bool tryBounce(ProjectileController instance, in ProjectileImpactInfo impactInfo)
                {
                    return instance && instance.TryGetComponent(out ProjectileEnvironmentBounceBehavior projectileBounceBehavior) && projectileBounceBehavior.TryBounce(impactInfo);
                }

                ILLabel afterRetLbl = il.DefineLabel();

                cursor.Emit(OpCodes.Brfalse, afterRetLbl);
                cursor.Emit(OpCodes.Ret);

                cursor.MarkLabel(afterRetLbl);
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }

        static void AimThrowableBase_FireProjectile(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                              x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertySetter(typeof(FireProjectileInfo), nameof(FireProjectileInfo.fuseOverride)))))
            {
                c.EmitDelegate(modifyFuse);
                static float modifyFuse(float fuseOverride)
                {
                    return isBouncingEnabled ? Mathf.Max(10f, fuseOverride) : fuseOverride;
                }
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }

        [RequireComponent(typeof(ProjectileController))]
        sealed class ProjectileEnvironmentBounceBehavior : MonoBehaviour
        {
            static readonly PhysicMaterial _bouncyMaterial = new PhysicMaterial("ProjectileBounce")
            {
                bounciness = 0.85f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                staticFriction = 0f,
                dynamicFriction = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum
            };

            int _bouncesRemaining;

            ProjectileController _projectileController;
            ProjectileSimple _projectileSimple;
            ProjectileImpactExplosion _projectileImpactExplosion;
            ProjectileGrappleController _projectileGrappleController;
            EntityStateMachine _projectileStateMachine;

            Vector3 _lastVelocityDirection;
            Vector3 _lastAngularVelocity;
            Rigidbody _rigidbody;

            PhysicMaterialOverride _materialOverrideController;

            void Awake()
            {
                _bouncesRemaining = maxBounces;

                _projectileController = GetComponent<ProjectileController>();
                _projectileSimple = GetComponent<ProjectileSimple>();
                _rigidbody = GetComponent<Rigidbody>();
                _projectileImpactExplosion = GetComponent<ProjectileImpactExplosion>();
                _projectileGrappleController = GetComponent<ProjectileGrappleController>();
                _projectileStateMachine = GetComponent<EntityStateMachine>();

                _materialOverrideController = PhysicMaterialOverride.AddOverrideMaterial(gameObject, _bouncyMaterial);
            }

            void OnDestroy()
            {
                if (_materialOverrideController)
                {
                    _materialOverrideController.RemoveOverrideMaterial(_bouncyMaterial);
                }
            }

            void FixedUpdate()
            {
                if (_rigidbody)
                {
                    if (_rigidbody.velocity.sqrMagnitude > float.Epsilon)
                    {
                        _lastVelocityDirection = _rigidbody.velocity.normalized;
                    }

                    _lastAngularVelocity = _rigidbody.angularVelocity;
                }
            }

            void reflectAroundNormal(Vector3 normal)
            {
                if (_rigidbody)
                {
                    // Velocity may have been modified from the collision, so restore it from the previous frame
                    _rigidbody.angularVelocity = _lastAngularVelocity;

                    if (_lastVelocityDirection.sqrMagnitude > float.Epsilon)
                    {
                        _rigidbody.rotation = Util.QuaternionSafeLookRotation(Vector3.Reflect(_lastVelocityDirection, normal));

                        return;
                    }
                }

                transform.forward = Vector3.Reflect(transform.forward, normal);
            }

            public bool TryBounce(in ProjectileImpactInfo impactInfo)
            {
                if (!isBouncingEnabled || _bouncesRemaining <= 0 || hitEnemy(impactInfo))
                {
                    _bouncesRemaining = 0;

                    Destroy(this);
                    return false;
                }

                reflectAroundNormal(impactInfo.estimatedImpactNormal);

                if (_projectileSimple)
                {
                    _projectileSimple.stopwatch = 0f;
                }

                if (_projectileImpactExplosion)
                {
                    _projectileImpactExplosion.stopwatch = 0f;
                }

                if (_projectileGrappleController)
                {
                    if (_projectileStateMachine && _projectileStateMachine.state is ProjectileGrappleController.FlyState)
                    {
                        // Re-start fly state to reset the lifetime of the grapple, as if it was just fired again
                        _projectileStateMachine.SetNextState(new ProjectileGrappleController.FlyState());
                    }
                }

                _bouncesRemaining--;
                if (_bouncesRemaining <= 0)
                {
                    Destroy(this);
                }

                return true;
            }

            bool hitEnemy(in ProjectileImpactInfo impactInfo)
            {
                if (!_projectileController || !_projectileController.teamFilter)
                    return false;

                if (!impactInfo.collider || !impactInfo.collider.TryGetComponent(out HurtBox hurtBox))
                    return false;

                HealthComponent healthComponent = hurtBox.healthComponent;
                if (!healthComponent || !healthComponent.alive)
                    return false;

                return FriendlyFireManager.ShouldDirectHitProceed(healthComponent, _projectileController.teamFilter.teamIndex);
            }
        }
    }
}
