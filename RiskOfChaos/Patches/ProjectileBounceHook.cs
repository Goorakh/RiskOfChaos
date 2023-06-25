using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.Projectile;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ProjectileBounceHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Projectile.ProjectileController.Awake += ProjectileController_Awake;

            IL.RoR2.Projectile.ProjectileController.OnCollisionEnter += ProjectileController_tryBouncePatch;
            IL.RoR2.Projectile.ProjectileController.OnTriggerEnter += ProjectileController_tryBouncePatch;
        }

        static bool isBouncingEnabled => maxBounces > 0;

        static uint maxBounces
        {
            get
            {
                if (ProjectileModificationManager.Instance)
                {
                    return ProjectileModificationManager.Instance.NetworkedProjectileBounceCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        static void ProjectileController_Awake(On.RoR2.Projectile.ProjectileController.orig_Awake orig, ProjectileController self)
        {
            orig(self);

            if (!self.GetComponent<ProjectileEnvironmentBounceBehavior>())
            {
                self.gameObject.AddComponent<ProjectileEnvironmentBounceBehavior>();
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
                cursor.Emit(OpCodes.Ldloc, impactInfoLocalIndex);
                cursor.EmitDelegate((ProjectileController instance, ProjectileImpactInfo impactInfo) =>
                {
                    return instance && instance.TryGetComponent(out ProjectileEnvironmentBounceBehavior projectileBounceBehavior) && projectileBounceBehavior.TryBounce(impactInfo);
                });

                ILLabel afterRetLbl = il.DefineLabel();

                cursor.Emit(OpCodes.Brfalse, afterRetLbl);
                cursor.Emit(OpCodes.Ret);

                cursor.MarkLabel(afterRetLbl);
            }
        }

        [RequireComponent(typeof(ProjectileController))]
        class ProjectileEnvironmentBounceBehavior : MonoBehaviour
        {
            int _timesBounced;

            float _lastBounceTime = float.NegativeInfinity;

            ProjectileSimple _projectileSimple;

            Vector3 _lastVelocityDirection;
            Vector3 _lastAngularVelocity;
            Rigidbody _rigidbody;

            const float SPEED_MULTIPLIER_PER_BOUNCE = 0.7f;

            void Awake()
            {
                _projectileSimple = GetComponent<ProjectileSimple>();
                _rigidbody = GetComponent<Rigidbody>();
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
                        Quaternion newRotation = Util.QuaternionSafeLookRotation(Vector3.Reflect(_lastVelocityDirection, normal));
                        _rigidbody.rotation = newRotation;

                        if (_projectileSimple)
                        {
                            _rigidbody.velocity = newRotation * Vector3.forward * _projectileSimple.desiredForwardSpeed;
                        }

                        return;
                    }
                }

                transform.forward = Vector3.Reflect(transform.forward, normal);
            }

            public bool TryBounce(ProjectileImpactInfo impactInfo)
            {
                if (!isBouncingEnabled || _timesBounced >= maxBounces || _lastBounceTime >= Time.fixedTime - 0.1f)
                    return false;

                if (_projectileSimple)
                {
                    _projectileSimple.desiredForwardSpeed *= SPEED_MULTIPLIER_PER_BOUNCE;
                }

                reflectAroundNormal(impactInfo.estimatedImpactNormal);

                _timesBounced++;
                _lastBounceTime = Time.fixedTime;
                return true;
            }
        }
    }
}
