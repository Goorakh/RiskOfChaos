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

        public int Compare(object x, object y)
        {
            if (x is ChaosEffectInfo effectInfo)
            {
                if (y is string str)
                {
                    return StringComparer.OrdinalIgnoreCase.Compare(effectInfo.Identifier, str);
                }
                else
                {
                    throw new ArgumentException($"parameter must be of type {nameof(String)}", nameof(y));
                }
            }
            else
            {
                throw new ArgumentException($"parameter must be of type {nameof(ChaosEffectInfo)}", nameof(x));
            }
        }
    }
}
