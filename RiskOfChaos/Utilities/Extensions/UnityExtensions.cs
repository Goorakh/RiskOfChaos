using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class UnityExtensions
    {
        public static void SetDontDestroyOnLoad(this GameObject obj, bool dontDestroyOnLoad)
        {
            if (dontDestroyOnLoad)
            {
                if (!obj.GetComponent<SetDontDestroyOnLoad>())
                {
                    obj.AddComponent<SetDontDestroyOnLoad>();
                }
            }
            else
            {
                if (obj.TryGetComponent(out SetDontDestroyOnLoad setDontDestroyOnLoad))
                {
                    GameObject.Destroy(setDontDestroyOnLoad);
                }

                if (Util.IsDontDestroyOnLoad(obj))
                {
                    SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
                }
            }
        }
    }
}
