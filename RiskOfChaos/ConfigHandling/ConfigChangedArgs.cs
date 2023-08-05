using System;

namespace RiskOfChaos.ConfigHandling
{
    public class ConfigChangedArgs<T> : EventArgs
    {
        public readonly ConfigHolder<T> Holder;
        public readonly T NewValue;

        public ConfigChangedArgs(ConfigHolder<T> configHolder)
        {
            Holder = configHolder;
            NewValue = Holder.Value;
        }
    }
}
