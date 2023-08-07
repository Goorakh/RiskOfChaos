using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RiskOfChaos.Utilities
{
    static class Empty<T>
    {
        public static readonly List<T> List = new List<T>();

        public static readonly ReadOnlyCollection<T> ReadOnlyCollection = new ReadOnlyCollection<T>(List);
    }
}
