using System;
using UnityEngine;

namespace RiskOfChaos.Content
{
    public sealed class PrefabInitializerArgs
    {
        public GameObject Prefab { get; }

        public IProgress<float> ProgressReceiver { get; }

        public PrefabInitializerArgs(GameObject prefab, IProgress<float> progressReceiver)
        {
            Prefab = prefab;
            ProgressReceiver = progressReceiver;
        }
    }
}
