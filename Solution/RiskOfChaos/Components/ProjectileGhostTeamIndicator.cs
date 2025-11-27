using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Components
{
    public sealed class ProjectileGhostTeamIndicator : MonoBehaviour
    {
        public ProjectileGhostTeamProvider TeamProvider;

        public Renderer TargetRenderer;

        public AssetReferenceT<Material> AllyMaterialAddress = new AssetReferenceT<Material>(string.Empty);
        public AssetReferenceT<Material> EnemyMaterialAddress = new AssetReferenceT<Material>(string.Empty);

        AssetOrDirectReference<Material> _allyMaterialReference;
        AssetOrDirectReference<Material> _enemyMaterialReference;

        bool _useAllyMaterial;
        bool _useEnemyMaterial;

        void Awake()
        {
            if (!TeamProvider)
            {
                TeamProvider = GetComponentInParent<ProjectileGhostTeamProvider>();
            }

            if (AllyMaterialAddress != null && AllyMaterialAddress.RuntimeKeyIsValid())
            {
                _allyMaterialReference = new AssetOrDirectReference<Material>
                {
                    unloadType = AsyncReferenceHandleUnloadType.AtWill,
                };

                _allyMaterialReference.onValidReferenceDiscovered += onAllyMaterialDiscovered;
                _allyMaterialReference.onValidReferenceLost += onAllyMaterialLost;

                _allyMaterialReference.address = AllyMaterialAddress;
            }

            if (EnemyMaterialAddress != null && EnemyMaterialAddress.RuntimeKeyIsValid())
            {
                _enemyMaterialReference = new AssetOrDirectReference<Material>
                {
                    unloadType = AsyncReferenceHandleUnloadType.AtWill,
                };

                _enemyMaterialReference.onValidReferenceDiscovered += onEnemyMaterialDiscovered;
                _enemyMaterialReference.onValidReferenceLost += onEnemyMaterialLost;

                _enemyMaterialReference.address = EnemyMaterialAddress;
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

        void OnDestroy()
        {
            _allyMaterialReference?.Reset();
            _enemyMaterialReference?.Reset();
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

            bool isEnemyTeam;
            switch (teamIndex)
            {
                case TeamIndex.Neutral:
                case TeamIndex.Player:
                    isEnemyTeam = false;
                    break;
                default:
                    isEnemyTeam = true;
                    break;
            }

            _useEnemyMaterial = isEnemyTeam;
            _useAllyMaterial = !isEnemyTeam;

            Material resolvedMaterial = null;

            if (_useEnemyMaterial)
            {
                resolvedMaterial = _enemyMaterialReference?.Result;
            }
            else if (_useAllyMaterial)
            {
                resolvedMaterial = _allyMaterialReference?.Result;
            }

            TargetRenderer.sharedMaterial = resolvedMaterial;
            TargetRenderer.enabled = resolvedMaterial;
        }

        void onAllyMaterialDiscovered(Material material)
        {
            if (_useAllyMaterial)
            {
                TargetRenderer.sharedMaterial = material;
                TargetRenderer.enabled = true;
            }
        }

        void onAllyMaterialLost(Material material)
        {
            if (_useAllyMaterial)
            {
                TargetRenderer.sharedMaterial = null;
                TargetRenderer.enabled = false;
            }
        }

        void onEnemyMaterialDiscovered(Material material)
        {
            if (_useEnemyMaterial)
            {
                TargetRenderer.sharedMaterial = material;
                TargetRenderer.enabled = true;
            }
        }

        void onEnemyMaterialLost(Material material)
        {
            if (_useEnemyMaterial)
            {
                TargetRenderer.sharedMaterial = null;
                TargetRenderer.enabled = false;
            }
        }
    }
}
