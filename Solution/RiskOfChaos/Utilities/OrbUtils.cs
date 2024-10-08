﻿using RiskOfChaos.Content.Orbs;
using RiskOfChaos.Utilities.Extensions;
using RoR2.Orbs;
using System;

namespace RiskOfChaos.Utilities
{
    public static class OrbUtils
    {
        public static bool IsTransferOrb(Orb orb)
        {
            switch (orb)
            {
                case ItemTransferOrb:
                case EquipmentTransferOrb:
                    return true;
                default:
                    return false;
            }
        }

        public static Orb Clone(Orb src)
        {
            Orb newOrb;
            try
            {
                newOrb = src switch
                {
                    ChainGunOrb srcChainGunOrb => new ChainGunOrb(srcChainGunOrb.orbEffectPrefab),
                    _ => (Orb)Activator.CreateInstance(src.GetType()),
                };
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to create new orb from {src}: {ex}");
                return null;
            }

            src.ShallowCopy(ref newOrb);

            return newOrb;
        }
    }
}
