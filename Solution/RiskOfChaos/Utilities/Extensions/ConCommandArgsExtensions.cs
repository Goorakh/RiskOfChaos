﻿using RiskOfChaos.EffectHandling;
using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ConCommandArgsExtensions
    {
        public static ChaosEffectIndex? TryGetArgChaosEffectIndex(in this ConCommandArgs args, int index)
        {
            string effectName = args.TryGetArgString(index);
            if (string.IsNullOrEmpty(effectName))
                return null;

            ChaosEffectIndex effectIndex = ChaosEffectCatalog.FindEffectIndex(effectName);
            if (effectIndex == ChaosEffectIndex.Invalid)
                return null;

            return effectIndex;
        }

        public static ChaosEffectIndex GetArgChaosEffectIndex(in this ConCommandArgs args, int index)
        {
            ChaosEffectIndex? effectIndex = args.TryGetArgChaosEffectIndex(index);
            if (!effectIndex.HasValue)
            {
                throw new ConCommandException($"No effect found for identifier '{args.TryGetArgString(index)}'");
            }

            return effectIndex.Value;
        }
    }
}
