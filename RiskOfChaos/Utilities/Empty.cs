using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RiskOfChaos.Utilities
{
    static class Empty<T>
    {
        public static readonly ReadOnlyCollection<T> ReadOnlyCollection = new ReadOnlyCollection<T>([]);
    }
}
