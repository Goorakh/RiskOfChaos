using System;
using System.Collections;

namespace RiskOfChaos.Utilities
{
    public readonly struct AsyncOperationCoroutineProgressReporter : IEnumerator
    {
        readonly IAsyncOperationCoroutine _coroutine;

        readonly IProgress<float> _progressReceiver;

        public AsyncOperationCoroutineProgressReporter(IAsyncOperationCoroutine asyncOperationHandle, IProgress<float> progressReceiver)
        {
            _coroutine = asyncOperationHandle;
            _progressReceiver = progressReceiver;
        }

        object IEnumerator.Current => _coroutine.Current;

        bool IEnumerator.MoveNext()
        {
            _progressReceiver.Report(_coroutine.Progress);
            return _coroutine.MoveNext();
        }

        void IEnumerator.Reset()
        {
            _coroutine.Reset();
        }
    }
}
