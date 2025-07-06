using HG.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class AsyncOperationExtensions
    {
        public static IEnumerator WaitForAllLoaded(this IEnumerable<AsyncOperationHandle> operations)
        {
            List<AsyncOperationHandle> operationHandles = new List<AsyncOperationHandle>(operations);

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
    }
}
