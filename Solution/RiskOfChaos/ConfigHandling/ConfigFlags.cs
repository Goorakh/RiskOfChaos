using System;

namespace RiskOfChaos.ConfigHandling
{
    [Flags]
    public enum ConfigFlags : byte
    {
        None = 0,
        Networked = 1 << 0,
    }
}
