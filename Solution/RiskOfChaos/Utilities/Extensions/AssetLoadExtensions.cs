using RoR2.ContentManagement;
using System;
using System.Diagnostics;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class AssetLoadExtensions
    {
        public static void OnSuccess<T>(this in AsyncOperationHandle<T> handle, Action<T> onSuccess)
        {
            if (handle.IsDone)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    string name = handle.DebugName;
                    if (string.IsNullOrEmpty(name))
                        name = handle.LocationName;

                    Log.Error($"Failed to load asset '{name}'"
#if DEBUG
                        + $". {new StackTrace()}"
#endif
                        );
                }
                else
                {
                    onSuccess(handle.Result);
                }

                return;
            }

#if DEBUG
            StackTrace stackTrace = new StackTrace();
#endif

            handle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    string name = handle.DebugName;
                    if (string.IsNullOrEmpty(name))
                        name = handle.LocationName;

                    Log.Error($"Failed to load asset '{name}'"
#if DEBUG
                        + $". {stackTrace}"
#endif
                        );

                    return;
                }

                onSuccess(handle.Result);
            };
        }

        public static T WaitForAsset<T>(this AssetOrDirectReference<T> assetReference) where T : UnityEngine.Object
        {
            if (assetReference is null)
                throw new ArgumentNullException(nameof(assetReference));

            if (assetReference.IsLoaded())
                return assetReference.Result;

            if (assetReference.directRef)
                return assetReference.directRef;

            if (assetReference.address != null && assetReference.address.RuntimeKeyIsValid())
            {
                if (!assetReference.loadHandle.IsValid())
                {
                    assetReference.LoadAsync();
                }

                return assetReference.WaitForCompletion();
            }

            return null;
        }

        public static void CallOnLoaded<T>(this AssetOrDirectReference<T> assetReference, Action<T> onLoaded) where T : UnityEngine.Object
        {
            if (assetReference is null)
                throw new ArgumentNullException(nameof(assetReference));

            if (onLoaded is null)
                throw new ArgumentNullException(nameof(onLoaded));

            if (assetReference.IsLoaded())
            {
                onLoaded(assetReference.Result);
            }
            else if (assetReference.directRef)
            {
                onLoaded(assetReference.directRef);
            }
            else if (assetReference.address != null && assetReference.address.RuntimeKeyIsValid())
            {
                assetReference.onValidReferenceDiscovered += onValidReferenceDiscovered;

                void onValidReferenceDiscovered(T asset)
                {
                    assetReference.onValidReferenceDiscovered -= onValidReferenceDiscovered;

                    onLoaded(asset);
                }

                if (!assetReference.loadHandle.IsValid())
                {
                    assetReference.LoadAsync();
                }
            }
            else
            {
                Log.Error($"Invalid asset reference, onLoaded callback will never be called! {new StackTrace()}");
            }
        }
    }
}
