using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void OnSuccess<T>(this in AsyncOperationHandle<T> handle, Action<T> onSuccess)
        {
#if DEBUG
            StackTrace stackTrace = new StackTrace();
#endif

            handle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    Log.Error($"Failed to load asset '{handle.LocationName}'"
#if DEBUG
                        + $". at {stackTrace}"
#endif
                        );

                    return;
                }

                onSuccess(handle.Result);
            };
        }
    }
}
