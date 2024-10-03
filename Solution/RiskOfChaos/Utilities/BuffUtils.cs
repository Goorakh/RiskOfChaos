using RiskOfChaos.Collections.CatalogIndex;
using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class BuffUtils
    {
        static readonly BuffIndexCollection _isDebuffOverrideList = new BuffIndexCollection([
            // MysticsItems compat
            "MysticsItems_Crystallized",
            "MysticsItems_TimePieceSlow",

            // Starstorm2 compat
            "bdMULENet",

            "bdBlinded",
        ]);

        public static bool IsDebuff(BuffDef buff)
        {
            if (buff.isDebuff)
                return true;

            if (_isDebuffOverrideList.Contains(buff.buffIndex))
                return true;

            return false;
        }

        static readonly BuffIndexCollection _isCooldownOverrideList = new BuffIndexCollection([
            // LostInTransit compat
            "RepulsionArmorCD",

            // Starstorm2 compat
            "BuffTerminationCooldown",
        ]);

        public static bool IsCooldown(BuffDef buff)
        {
            if (buff.isCooldown)
                return true;

            if (_isCooldownOverrideList.Contains(buff.buffIndex))
                return true;

            return false;
        }

        public static bool IsDOT(BuffDef buff)
        {
            foreach (DotController.DotDef dotDef in DotController.dotDefs)
            {
                if (dotDef == null)
                    continue;

                if (dotDef.associatedBuff == buff)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
