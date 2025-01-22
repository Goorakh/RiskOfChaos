using EntityStates.Loader;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class ReplacedProjectileHookReferenceResolver : MonoBehaviour
    {
        ProjectileController _projectileController;

        void Awake()
        {
            _projectileController = GetComponent<ProjectileController>();
        }

        void Start()
        {
            resolveHookReference();
        }

        void resolveHookReference()
        {
            if (_projectileController && _projectileController.owner)
            {
                EntityStateMachine hookStateMachine = EntityStateMachine.FindByCustomName(_projectileController.owner, "Hook");
                if (hookStateMachine)
                {
                    if (hookStateMachine.state is FireHook fireHookState)
                    {
                        // Just pretend this projectile is a hook. Assuming the entity state properly nullchecks components this will work fine.
                        fireHookState.SetHookReference(gameObject);
                    }
                }
            }
        }
    }
}
