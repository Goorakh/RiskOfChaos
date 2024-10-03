using RoR2;
using UnityEngine;

namespace RiskOfChaos.Components
{
    [RequireComponent(typeof(CharacterBody))]
    public sealed class KeepBuff : MonoBehaviour
    {
        public BuffIndex BuffIndex = BuffIndex.None;
        public int MinBuffCount = -1;
    }
}
