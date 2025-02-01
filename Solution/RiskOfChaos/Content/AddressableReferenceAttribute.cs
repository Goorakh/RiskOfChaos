using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class AddressableReferenceAttribute : HG.Reflection.SearchableAttribute
    {
        [SystemInitializer]
        static IEnumerator Init()
        {
            List<IEnumerator> loadOperations = [];

            List<AddressableReferenceAttribute> addressableReferenceAttributes = [];
            GetInstances(addressableReferenceAttributes);

            loadOperations.EnsureCapacity(addressableReferenceAttributes.Count);

            foreach (AddressableReferenceAttribute addressableReferenceAttribute in addressableReferenceAttributes)
            {
                loadOperations.Add(addressableReferenceAttribute.AssignValueAsync());
            }

            if (loadOperations.Count > 0)
            {
                yield return loadOperations.WaitForAllComplete();
            }
        }

        public string AssetPath { get; }

        readonly Type _explicitAssetType;

        public Type AssetType
        {
            get
            {
                if (_explicitAssetType != null)
                    return _explicitAssetType;

                switch (target)
                {
                    case FieldInfo fieldInfo:
                        return fieldInfo.FieldType;
                    case PropertyInfo propertyInfo:
                        return propertyInfo.PropertyType;
                }

                return null;
            }
        }

        public new MemberInfo target => base.target as MemberInfo;

        public AddressableReferenceAttribute(string assetPath)
        {
            AssetPath = assetPath;
        }

        public AddressableReferenceAttribute(string assetPath, Type explicitAssetType) : this(assetPath)
        {
            _explicitAssetType = explicitAssetType;
        }

        public bool Validate()
        {
            switch (target)
            {
                case FieldInfo fieldInfo:

                    if (!fieldInfo.IsLiteral)
                    {
                        Log.Error($"Attribute cannot apply to constant field {fieldInfo.DeclaringType.FullName}.{fieldInfo.Name}");
                        return false;
                    }

                    if (!fieldInfo.IsStatic)
                    {
                        Log.Error($"Attribute cannot to non-static field {fieldInfo.DeclaringType.FullName}.{fieldInfo.Name}");
                        return false;
                    }

                    return true;
                case PropertyInfo propertyInfo:

                    MethodInfo setMethod = propertyInfo.SetMethod;

                    if (setMethod == null)
                    {
                        Log.Error($"Attribute cannot apply to readonly property {propertyInfo.DeclaringType.FullName}.{propertyInfo.Name}");
                        return false;
                    }

                    if (!setMethod.IsStatic)
                    {
                        Log.Error($"Attribute cannot apply non-static property {propertyInfo.DeclaringType.FullName}.{propertyInfo.Name}");
                    }

                    return true;
                default:
                    Log.Error($"Invalid member type '{target?.GetType()}'");
                    return false;
            }
        }

        public IEnumerator AssignValueAsync()
        {
            AsyncOperationHandle<IList<IResourceLocation>> locationLoadHandle = Addressables.LoadResourceLocationsAsync(AssetPath, AssetType);
            while (!locationLoadHandle.IsDone)
            {
                yield return null;
            }

            if (locationLoadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error($"Failed to load asset location '{AssetType.FullName}' at '{AssetPath}': {locationLoadHandle.OperationException}");
                yield break;
            }

            IResourceLocation assetLocation = locationLoadHandle.Result[0];

            AsyncOperationHandle<object> assetLoadHandle = Addressables.LoadAssetAsync<object>(assetLocation);
            while (!assetLoadHandle.IsDone)
            {
                yield return null;
            }

            if (assetLoadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error($"Failed to load asset at {assetLocation.PrimaryKey}: {assetLoadHandle.OperationException}");
                yield break;
            }

            setFieldValue(assetLoadHandle.Result);
            Log.Debug($"Assigned asset {assetLocation.PrimaryKey} to {target.DeclaringType.FullName}.{target.Name} ({AssetType.FullName})");
        }

        void setFieldValue(object value)
        {
            switch (target)
            {
                case FieldInfo fieldInfo:
                    fieldInfo.SetValue(null, value);
                    break;
                case PropertyInfo propertyInfo:
                    propertyInfo.SetValue(null, value);
                    break;
            }
        }
    }
}
