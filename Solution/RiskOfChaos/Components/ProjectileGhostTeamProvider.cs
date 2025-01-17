using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class ProjectileGhostTeamProvider : MonoBehaviour
    {
        [SerializeField]
        TeamIndex _teamIndex = TeamIndex.None;
        public TeamIndex TeamIndex
        {
            get
            {
                return _teamIndex;
            }
            set
            {
                if (_teamIndex == value)
                    return;

                _teamIndex = value;
                OnTeamChanged?.Invoke();
            }
        }

        public event Action OnTeamChanged;
    }
}
