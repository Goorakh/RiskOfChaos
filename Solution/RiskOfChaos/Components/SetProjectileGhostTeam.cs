using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class SetProjectileGhostTeam : MonoBehaviour
    {
        ProjectileController _projectileController;
        TeamFilter _teamFilter;
        TeamComponent _teamComponent;

        void Awake()
        {
            _projectileController = GetComponent<ProjectileController>();
            _teamFilter = GetComponentInChildren<TeamFilter>();
            _teamComponent = GetComponentInChildren<TeamComponent>();
        }

        void Start()
        {
            if (!_teamFilter && !_teamComponent)
            {
                Log.Error($"No TeamFilter or TeamComponent available on projectile {_projectileController.gameObject}");
                return;
            }

            TeamIndex teamIndex = TeamIndex.None;
            if (_teamFilter)
            {
                teamIndex = _teamFilter.teamIndex;
            }
            else if (_teamComponent)
            {
                teamIndex = _teamComponent.teamIndex;
            }

            if (_projectileController.ghost && _projectileController.ghost.TryGetComponent(out ProjectileGhostTeamProvider ghostTeamProvider))
            {
                ghostTeamProvider.TeamIndex = teamIndex;
            }
        }
    }
}
