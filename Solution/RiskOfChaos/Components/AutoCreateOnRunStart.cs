using RiskOfChaos.Content;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    [DisallowMultipleComponent]
    public sealed class AutoCreateOnRunStart : MonoBehaviour
    {
        static GameObject[] _allAutoCreateNetworkedPrefabs = [];
        static GameObject[] _allAutoCreatePrefabs = [];

        [SystemInitializer]
        static void Init()
        {
            GameObject[] allPrefabs = [.. RoCContent.NetworkedPrefabs.AllPrefabs, .. RoCContent.LocalPrefabs.AllPrefabs];

            List<GameObject> autoCreateNetworkedPrefabs = new List<GameObject>(allPrefabs.Length);
            List<GameObject> autoCreatePrefabs = new List<GameObject>(allPrefabs.Length);

            foreach (GameObject prefab in allPrefabs)
            {
                if (prefab.GetComponent<AutoCreateOnRunStart>())
                {
                    List<GameObject> prefabList;
                    if (prefab.GetComponent<NetworkIdentity>())
                    {
                        prefabList = autoCreateNetworkedPrefabs;
                    }
                    else
                    {
                        prefabList = autoCreatePrefabs;
                    }

                    prefabList.Add(prefab);
                }
            }

            _allAutoCreateNetworkedPrefabs = [.. autoCreateNetworkedPrefabs];
            _allAutoCreatePrefabs = [.. autoCreatePrefabs];

            if (_allAutoCreateNetworkedPrefabs.Length > 0 || _allAutoCreatePrefabs.Length > 0)
            {
                Run.onRunStartGlobal += onRunStartGlobal;
            }
        }

        static void onRunStartGlobal(Run run)
        {
            if (NetworkServer.active)
            {
                foreach (GameObject networkedPrefab in _allAutoCreateNetworkedPrefabs)
                {
                    NetworkServer.Spawn(Instantiate(networkedPrefab));

                    Log.Debug($"Spawned networked prefab {networkedPrefab.name}");
                }
            }

            foreach (GameObject prefab in _allAutoCreatePrefabs)
            {
                Instantiate(prefab);

                Log.Debug($"Spawned prefab {prefab.name}");
            }
        }
    }
}
