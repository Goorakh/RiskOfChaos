using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    // Logspams due to being called in FixedUpdate with no null check
    static class FixDynamicBoneApplyParticlesNullRef
    {
        static float _lastPreventTime = float.NegativeInfinity;

        [SystemInitializer]
        static void Init()
        {
            On.DynamicBone.ApplyParticlesToTransforms += DynamicBone_ApplyParticlesToTransforms;
        }

        static void DynamicBone_ApplyParticlesToTransforms(On.DynamicBone.orig_ApplyParticlesToTransforms orig, DynamicBone self)
        {
            // Doesn't actually fix the issue, just prevents it from logspamming.
            try
            {
                orig(self);
            }
            catch (NullReferenceException e)
            {
                bool shouldLog = Time.time > _lastPreventTime + 0.5f;
                _lastPreventTime = Time.time;

                // Only log if it doesn't happen every frame
                if (shouldLog)
                {
                    throw e;
                }
            }
        }
    }
}
