using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Utilities.Assets
{
    public sealed class AssetLoadOperation<T> : IAsyncOperationCoroutine where T : UnityEngine.Object
    {
        readonly AssetBundleRequest _assetBundleRequest;

        public AssetLoadOperation(AssetBundleRequest assetBundleRequest)
        {
            _assetBundleRequest = assetBundleRequest;
            _assetBundleRequest.completed += onCompleted;
        }

        public T Result => _assetBundleRequest.asset as T;

        public float Progress => _assetBundleRequest.progress;

        public bool IsDone => _assetBundleRequest.isDone;

        public delegate void AssetLoadCompleteDelegate(T asset);
        event AssetLoadCompleteDelegate onComplete;
        public event AssetLoadCompleteDelegate OnComplete
        {
            add
            {
                if (IsDone)
                {
                    value?.Invoke(Result);
                }
                else
                {
                    onComplete += value;
                }
            }
            remove
            {
                onComplete -= value;
            }
        }

        void onCompleted(AsyncOperation _)
        {
            _assetBundleRequest.completed -= onCompleted;
            onComplete?.Invoke(Result);
        }

        object IEnumerator.Current => null;

        bool IEnumerator.MoveNext()
        {
            return !IsDone;
        }

        void IEnumerator.Reset()
        {
        }
    }
}
