using System.Collections;

namespace RiskOfChaos.Utilities
{
    public interface IAsyncOperationCoroutine : IEnumerator
    {
        float Progress { get; }
    }
}
