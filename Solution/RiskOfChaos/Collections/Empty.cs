using System;
using System.Collections.ObjectModel;

namespace RiskOfChaos.Collections
{
    static class Empty<T>
    {
        public static readonly ReadOnlyCollection<T> ReadOnlyCollection = new ReadOnlyCollection<T>(Array.Empty<T>());
    }
}
