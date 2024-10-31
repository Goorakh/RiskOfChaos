using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Utilities.Assets
{
    public class AssetLoadOperation<T> : IEnumerator where T : UnityEngine.Object
    {
        readonly AssetBundleRequest _assetBundleRequest;

        public AssetLoadOperation(AssetBundleRequest assetBundleRequest)
        {
            _assetBundleRequest = assetBundleRequest;
            _assetBundleRequest.completed += onCompleted;
        }

        public T Result => _assetBundleRequest.asset as T;

        public bool IsDone => _assetBundleRequest.isDone;

        public delegate void AssetLoadCompleteDelegate(T asset);
        public event AssetLoadCompleteDelegate OnComplete;

        void onCompleted(AsyncOperation _)
        {
            _assetBundleRequest.completed -= onCompleted;
            OnComplete?.Invoke(Result);
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
