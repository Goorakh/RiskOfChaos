using RoR2.ContentManagement;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities
{
    public static class AddressableUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncOperationHandle<T> LoadAssetAsync<T>(string assetGuid, AsyncReferenceHandleUnloadType unloadType = AsyncReferenceHandleUnloadType.AtWill) where T : UnityEngine.Object
        {
            return AssetAsyncReferenceManager<T>.LoadAsset(new AssetReferenceT<T>(assetGuid), unloadType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncOperationHandle<T> LoadAssetAsync<T>(AssetReferenceT<T> assetReference, AsyncReferenceHandleUnloadType unloadType = AsyncReferenceHandleUnloadType.AtWill) where T : UnityEngine.Object
        {
            return AssetAsyncReferenceManager<T>.LoadAsset(assetReference, unloadType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncOperationHandle<T> LoadTempAssetAsync<T>(string assetGuid, AsyncReferenceHandleUnloadType unloadType = AsyncReferenceHandleUnloadType.AtWill) where T : UnityEngine.Object
        {
            return LoadTempAssetAsync(new AssetReferenceT<T>(assetGuid), unloadType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncOperationHandle<T> LoadTempAssetAsync<T>(AssetReferenceT<T> assetReference, AsyncReferenceHandleUnloadType unloadType = AsyncReferenceHandleUnloadType.AtWill) where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> loadHandle = AssetAsyncReferenceManager<T>.LoadAsset(assetReference, unloadType);
            loadHandle.Completed += _ =>
            {
                AssetAsyncReferenceManager<T>.UnloadAsset(assetReference);
            };

            return loadHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnloadAsset<T>(AssetReferenceT<T> assetReference) where T : UnityEngine.Object
        {
            AssetAsyncReferenceManager<T>.UnloadAsset(assetReference);
        }
    }
}
