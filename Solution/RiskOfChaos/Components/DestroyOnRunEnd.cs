using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    [DisallowMultipleComponent]
    public class DestroyOnRunEnd : MonoBehaviour
    {
        void Awake()
        {
            Run.onRunDestroyGlobal += onRunEnd;
        }

        void OnDestroy()
        {
            Run.onRunDestroyGlobal -= onRunEnd;
        }

        void onRunEnd(Run _)
        {
            Destroy(gameObject);
        }
    }
}
