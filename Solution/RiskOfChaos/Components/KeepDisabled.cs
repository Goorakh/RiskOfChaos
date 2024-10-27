using UnityEngine;

namespace RiskOfChaos.Components
{
    public sealed class KeepDisabled : MonoBehaviour
    {
        void OnEnable()
        {
            gameObject.SetActive(false);
        }
    }
}
