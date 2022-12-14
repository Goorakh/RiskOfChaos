using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos
{
    public static class RoCMath
    {
        public static float CalcReductionWeight(int count, float coeff)
        {
            return coeff / (count + coeff);
        }
    }
}
