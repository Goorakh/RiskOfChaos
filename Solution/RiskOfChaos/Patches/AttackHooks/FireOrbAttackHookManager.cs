using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    sealed class FireOrbAttackHookManager : AttackHookManager
    {
        readonly OrbManager _orbManager;
        readonly Orb _originalOrb;
        readonly Orb _orbTemplate;

        protected override AttackInfo AttackInfo { get; }

        public FireOrbAttackHookManager(OrbManager orbManager, Orb orb)
        {
            _orbManager = orbManager;
            _originalOrb = orb;
            _orbTemplate = OrbUtils.Clone(orb);

            AttackInfo = new AttackInfo(orb);
        }

        OrbManager getOrbManager()
        {
            if (_orbManager)
                return _orbManager;

            return OrbManager.instance;
        }

        protected override void fireAttackCopy(AttackInfo attackInfo)
        {
            OrbManager orbManager = getOrbManager();
            if (!orbManager)
                return;

            Orb orb = OrbUtils.Clone(_orbTemplate);
            attackInfo.PopulateOrb(orb);
            orbManager.AddOrb(orb);
        }

        protected override bool tryFireRepeating()
        {
            if (OrbUtils.IsTransferOrb(_orbTemplate))
                return false;

            if (_orbTemplate.TryGetBouncedObjects(out ReadOnlyCollection<HealthComponent> bouncedObjects) && bouncedObjects.Count > 0)
                return false;

            if (OrbBounceHook.IsBouncedOrb(_originalOrb))
                return false;

            return base.tryFireRepeating();
        }

        protected override bool tryFireBounce()
        {
            if (OrbUtils.IsTransferOrb(_originalOrb) || _originalOrb is VoidLightningOrb)
                return false;

            if (_orbTemplate.TryGetProcChainMask(out ProcChainMask orbProcChain))
            {
                if (!orbProcChain.HasModdedProc(CustomProcTypes.Bouncing) && orbProcChain.HasAnyProc())
                {
                    return false;
                }
            }

            return OrbBounceHook.TryStartBounceOrb(_originalOrb, AttackInfo);
        }

        protected override bool tryReplace()
        {
            return !OrbUtils.IsTransferOrb(_originalOrb) && AttackInfo.Position != Vector3.zero && base.tryReplace();
        }
    }
}
