using HG;
using HG.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class AsyncOperationExtensions
    {
        public static IEnumerator WaitForAllLoaded(this IEnumerable<AsyncOperationHandle> operations)
        {
            List<AsyncOperationHandle> operationHandles = [.. operations];

            while (operationHandles.Count > 0)
            {
                yield return null;

                for (int i = operationHandles.Count - 1; i >= 0; i--)
                {
                    if (operationHandles[i].IsDone)
                    {
                        operationHandles.RemoveAt(i);
                    }
                }
            }
        }

        public static IEnumerator WaitForAllComplete(this IEnumerable<IEnumerator> operations)
        {
            List<IEnumerator> operationsList = [.. operations];

            while (operationsList.Count > 0)
            {
                for (int i = operationsList.Count - 1; i >= 0; i--)
                {
                    if (operationsList[i].MoveNext())
                    {
                        yield return operationsList[i].Current;
                    }
                    else
                    {
                        operationsList.RemoveAt(i);
                    }
                }
            }
        }

        public static void AddRange<T>(this ParallelCoroutine parallelCoroutine, IEnumerable<T> coroutines) where T : IEnumerator
        {
            if (parallelCoroutine is null)
                throw new ArgumentNullException(nameof(parallelCoroutine));

            if (coroutines is null)
                throw new ArgumentNullException(nameof(coroutines));

            foreach (T coroutine in coroutines)
            {
                parallelCoroutine.Add(coroutine);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncOperationCoroutineProgressReporter AsProgressCoroutine<T>(this AsyncOperationHandle<T> asyncOperationHandle, IProgress<float> progressReceiver)
        {
            return AsProgressCoroutine((AsyncOperationHandle)asyncOperationHandle, progressReceiver);
        }

        public static AsyncOperationCoroutineProgressReporter AsProgressCoroutine(this AsyncOperationHandle asyncOperationHandle, IProgress<float> progressReceiver)
        {
            return new AsyncOperationCoroutineProgressReporter(new AsyncOperationCoroutineWrapper(asyncOperationHandle), progressReceiver);
        }

        public static void Add(this ParallelProgressCoroutine parallelProgressCoroutine, AsyncOperationHandle asyncOperationHandle)
        {
            ReadableProgress<float> progress = new ReadableProgress<float>();
            parallelProgressCoroutine.Add(asyncOperationHandle.AsProgressCoroutine(progress), progress);
        }

        public static void Add(this ParallelProgressCoroutine parallelProgressCoroutine, IAsyncOperationCoroutine asyncCoroutine)
        {
            ReadableProgress<float> progress = new ReadableProgress<float>();
            parallelProgressCoroutine.Add(new AsyncOperationCoroutineProgressReporter(asyncCoroutine, progress), progress);
        }
    }
}
