using System;
using System.Collections;

namespace RiskOfChaos.EffectHandling
{
    public class ChaosEffectInfoIdentifierComparer : IComparer
    {
        public static readonly ChaosEffectInfoIdentifierComparer Instance = new ChaosEffectInfoIdentifierComparer();

        ChaosEffectInfoIdentifierComparer()
        {
        }

        static bool tryGetString(object param, out string str)
        {
            switch (param)
            {
                case string:
                    str = (string)param;
                    return true;
                case ChaosEffectInfo effectInfo:
                    str = effectInfo.Identifier;
                    return true;
            }

            str = null;
            return false;
        }

        public int Compare(object x, object y)
        {
            if (!tryGetString(x, out string strX))
                throw new ArgumentException($"Parameter was not of valid type", nameof(x));

            if (!tryGetString(y, out string strY))
                throw new ArgumentException($"Parameter was not of valid type", nameof(y));

            return StringComparer.OrdinalIgnoreCase.Compare(strX, strY);
        }
    }
}
