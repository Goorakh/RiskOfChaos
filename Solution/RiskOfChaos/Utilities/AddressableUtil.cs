using RoR2.ContentManagement;
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
    }
}
