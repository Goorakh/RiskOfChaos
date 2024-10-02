using System.Collections.Generic;

namespace RiskOfChaos.ConfigHandling
{
    public interface IConfigProvider
    {
        IEnumerable<ConfigHolderBase> GetConfigs();
    }
}
