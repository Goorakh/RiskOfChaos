using RiskOfChaos.Content.AssetCollections;
using System;
using System.Reflection;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class EntityStateTypeAttribute : Attribute
    {
        [ContentInitializer]
        static void InitContent(EntityStateAssetCollection entityStates)
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                EntityStateTypeAttribute entityStateTypeAttribute = type.GetCustomAttribute<EntityStateTypeAttribute>();
                if (entityStateTypeAttribute != null)
                {
                    entityStates.Add(type);
                }
            }
        }
    }
}
