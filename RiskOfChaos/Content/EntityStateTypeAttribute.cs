using HG.Reflection;
using RoR2.ContentManagement;
using System;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EntityStateTypeAttribute : SearchableAttribute
    {
        public static void AddStateTypesTo(NamedAssetCollection<Type> types)
        {
            types.Add(Array.ConvertAll(GetInstances<EntityStateTypeAttribute>().ToArray(), a => a.target as Type));
        }
    }
}
