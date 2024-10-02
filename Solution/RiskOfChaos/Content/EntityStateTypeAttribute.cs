using HG.Reflection;
using RoR2.ContentManagement;
using System;
using System.Linq;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EntityStateTypeAttribute : SearchableAttribute
    {
        public static void AddStateTypesTo(NamedAssetCollection<Type> types)
        {
            types.Add(GetInstances<EntityStateTypeAttribute>().Select(a => (Type)a.target).Distinct().ToArray());
        }
    }
}
