using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class ProjectileGhostTeamProvider : MonoBehaviour
    {
        TeamIndex _teamIndex;
        public TeamIndex TeamIndex
        {
            get
            {
                return _teamIndex;
            }
            set
            {
                _teamIndex = value;
                OnTeamChanged?.Invoke();
            }
        }

        public event Action OnTeamChanged;

        void OnEnable()
        {
            TeamIndex = TeamIndex.None;
        }
    }
}
