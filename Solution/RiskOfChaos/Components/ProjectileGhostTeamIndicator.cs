using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class ProjectileGhostTeamIndicator : MonoBehaviour
    {
        public ProjectileGhostTeamProvider TeamProvider;

        public RenderTeamConfiguration[] TeamConfigurations;

        void Awake()
        {
            if (!TeamProvider)
            {
                TeamProvider = GetComponentInParent<ProjectileGhostTeamProvider>();
            }
        }

        void OnEnable()
        {
            if (TeamProvider)
            {
                TeamProvider.OnTeamChanged += onTeamChanged;
            }

            refreshIndicators();
        }

        void OnDisable()
        {
            if (TeamProvider)
            {
                TeamProvider.OnTeamChanged -= onTeamChanged;
            }
        }

        void onTeamChanged()
        {
            refreshIndicators();
        }

        void refreshIndicators()
        {
            TeamIndex teamIndex = TeamIndex.None;
            if (TeamProvider)
            {
                teamIndex = TeamProvider.TeamIndex;
            }
            else
            {
                Log.Warning("Missing TeamProvider component");
            }

            foreach (RenderTeamConfiguration teamConfiguration in TeamConfigurations)
            {
                Renderer renderer = teamConfiguration.TargetRenderer;
                if (!renderer)
                    continue;

                Material[] materials = teamConfiguration.FallbackMaterials;
                foreach (TeamMaterialPair teamMaterial in teamConfiguration.TeamMaterials)
                {
                    if (teamMaterial.TeamIndex == teamIndex)
                    {
                        materials = teamMaterial.Materials;
                        break;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        [Serializable]
        public struct RenderTeamConfiguration
        {
            public Renderer TargetRenderer;
            public Material[] FallbackMaterials;
            public TeamMaterialPair[] TeamMaterials;

            public RenderTeamConfiguration(Renderer targetRenderer, Material[] fallbackMaterials, TeamMaterialPair[] teamMaterials)
            {
                TargetRenderer = targetRenderer;
                FallbackMaterials = fallbackMaterials;
                TeamMaterials = teamMaterials;
            }

            public RenderTeamConfiguration(Renderer targetRenderer, TeamMaterialPair[] teamMaterials) : this(targetRenderer, targetRenderer.sharedMaterials, teamMaterials)
            {
            }
        }

        [Serializable]
        public struct TeamMaterialPair
        {
            public TeamIndex TeamIndex;
            public Material[] Materials;

            public TeamMaterialPair(TeamIndex teamIndex, Material[] materials)
            {
                TeamIndex = teamIndex;
                Materials = materials;
            }

            public TeamMaterialPair(TeamIndex teamIndex, Material material) : this(teamIndex, [material])
            {
            }
        }
    }
}
