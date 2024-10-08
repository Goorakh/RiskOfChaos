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
                yield return 0;

                for (int i = operationHandles.Count - 1; i >= 0; i--)
                {
                    if (operationHandles[i].IsDone)
                    {
                        operationHandles.RemoveAt(i);
                    }
                }
            }
        }
    }
}
