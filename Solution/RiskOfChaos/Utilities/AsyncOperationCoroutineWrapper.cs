using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities
{
    public readonly struct AsyncOperationCoroutineWrapper : IAsyncOperationCoroutine
    {
        readonly AsyncOperationHandle _asyncOperationHandle;

        public AsyncOperationCoroutineWrapper(AsyncOperationHandle asyncOperationHandle)
        {
            _asyncOperationHandle = asyncOperationHandle;
        }

        public float Progress => _asyncOperationHandle.PercentComplete;

        object IEnumerator.Current => ((IEnumerator)_asyncOperationHandle).Current;

        bool IEnumerator.MoveNext()
        {
            return ((IEnumerator)_asyncOperationHandle).MoveNext();
        }

        void IEnumerator.Reset()
        {
            ((IEnumerator)_asyncOperationHandle).Reset();
        }
    }
}
