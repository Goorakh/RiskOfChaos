using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities
{
    public sealed class SpawnPoolEntry<T> where T : UnityEngine.Object
    {
        public float Weight { get; set; }

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_cachedAssetName))
                    return _cachedAssetName;

                if (!string.IsNullOrEmpty(_assetGuid))
                    return _assetGuid;

                return string.Empty;
            }
        }

        string _cachedAssetName;

        T _directReference;
        string _assetGuid;

        public readonly ReadOnlyArray<ExpansionIndex> RequiredExpansions;

        readonly Func<bool> _isAvailableFunc;

        AsyncOperationHandle _assetConversionLoadHandle;

        public bool IsValid => Weight > 0f && (_directReference || !string.IsNullOrEmpty(_assetGuid) || _assetConversionLoadHandle.IsValid());

        public bool IsAvailable => IsValid && ExpansionUtils.AllExpansionsEnabled(RequiredExpansions) && (_isAvailableFunc == null || _isAvailableFunc());

        SpawnPoolEntry(SpawnPoolEntryParameters parameters)
        {
            Weight = parameters.Weight;
            RequiredExpansions = parameters.RequiredExpansions;
            _isAvailableFunc = parameters.IsAvailableFunc;
        }

        public SpawnPoolEntry(T directRef, SpawnPoolEntryParameters parameters) : this(parameters)
        {
            if (!directRef)
                throw new ArgumentNullException(nameof(directRef));

            _directReference = directRef;
            _cachedAssetName = _directReference.ToString();
        }

        public SpawnPoolEntry(string assetGuid, SpawnPoolEntryParameters parameters) : this(parameters)
        {
            if (string.IsNullOrWhiteSpace(assetGuid) || !Guid.TryParse(assetGuid, out _))
                throw new ArgumentException($"'{nameof(assetGuid)}' must be a valid guid.", nameof(assetGuid));

            _assetGuid = assetGuid;
        }

        public AssetOrDirectReference<T> GetAssetReference(bool autoLoad = true)
        {
            if (_assetConversionLoadHandle.IsValid())
            {
                if (!_assetConversionLoadHandle.IsDone)
                {
                    _assetConversionLoadHandle.WaitForCompletion();
                }

                _assetConversionLoadHandle = default;
            }

            AssetOrDirectReference<T> assetReference = new AssetOrDirectReference<T>
            {
                unloadType = AsyncReferenceHandleUnloadType.OnSceneUnload,
                loadOnAssigned = autoLoad
            };

            if (!string.IsNullOrEmpty(_assetGuid))
            {
                assetReference.address = new AssetReferenceT<T>(_assetGuid);
            }
            else
            {
                assetReference.directRef = _directReference;
            }

            if (string.IsNullOrEmpty(_cachedAssetName))
            {
                if (assetReference.Result)
                {
                    _cachedAssetName = assetReference.Result.ToString();
                }
                else
                {
                    void onLoaded(T asset)
                    {
                        if (string.IsNullOrEmpty(_cachedAssetName))
                        {
                            _cachedAssetName = asset ? asset.ToString() : string.Empty;
                            Log.Debug($"Recorded asset name: {_cachedAssetName}");
                        }

                        assetReference.onValidReferenceDiscovered -= onLoaded;
                    }

                    assetReference.onValidReferenceDiscovered += onLoaded;
                }
            }

            return assetReference;
        }

        public bool MatchAssetReference(AssetOrDirectReference<T> assetReference)
        {
            if (assetReference == null)
                return false;

            if (!string.IsNullOrEmpty(_assetGuid))
                return string.Equals((assetReference.address?.RuntimeKey as string), _assetGuid);

            if (_directReference)
                return assetReference.directRef == _directReference;

            return false;
        }

        public override string ToString()
        {
            return $"{Name}: {Weight} ({string.Join(", ", RequiredExpansions)})";
        }

        public static SpawnPoolEntry<T> CreateConvertedAssetEntry<TAsset>(string assetGuid, Converter<TAsset, T> assetConverter, SpawnPoolEntryParameters parameters)
            where TAsset : UnityEngine.Object
        {
            SpawnPoolEntry<T> entry = new SpawnPoolEntry<T>(parameters);

            AsyncOperationHandle<TAsset> assetLoadHandle = AddressableUtil.LoadTempAssetAsync<TAsset>(assetGuid);
            assetLoadHandle.OnSuccess(asset =>
            {
                T convertedAsset = assetConverter(asset);

                if (typeof(T) == typeof(TAsset) && asset == convertedAsset)
                {
                    entry._assetGuid = assetGuid;
                }
                else
                {
                    entry._directReference = convertedAsset;
                }

                // Regardless if the converted asset is different or not,
                // we can still confidently cache the name here since it's already loaded
                entry._cachedAssetName = convertedAsset.ToString();
            });

            entry._assetConversionLoadHandle = assetLoadHandle;

            return entry;
        }
    }
}
