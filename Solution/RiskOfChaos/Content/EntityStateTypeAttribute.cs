using HG.Reflection;
using RiskOfChaos.Content.AssetCollections;
using System;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class EntityStateTypeAttribute : SearchableAttribute
    {
        [ContentInitializer]
        static void InitContent(EntityStateAssetCollection entityStates)
        {
            foreach (SearchableAttribute attribute in GetInstances<EntityStateTypeAttribute>())
            {
                entityStates.Add((Type)attribute.target);
            }
        }
    }
}
