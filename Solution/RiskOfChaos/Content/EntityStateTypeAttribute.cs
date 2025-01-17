using EntityStates;
using HG.Reflection;
using RiskOfChaos.Content.AssetCollections;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class EntityStateTypeAttribute : SearchableAttribute
    {
        [ContentInitializer]
        static void InitContent(EntityStateAssetCollection entityStates)
        {
            List<EntityStateTypeAttribute> attributes = [];
            GetInstances(attributes);
            foreach (EntityStateTypeAttribute attribute in attributes)
            {
                if (attribute.target is not Type entityStateType)
                {
                    Log.Error($"Invalid attribute target {attribute.target}: Must be a type that inherits from EntityState");
                    continue;
                }

                if (!typeof(EntityState).IsAssignableFrom(entityStateType))
                {
                    Log.Error($"Invalid attribute target {entityStateType}: Must inherit from EntityState");
                    continue;
                }

                entityStates.Add(entityStateType);
            }
        }
    }
}
