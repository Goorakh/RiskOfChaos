using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Content
{
    partial class RoCContent
    {
        partial class ProjectilePrefabs
        {
            [ContentInitializer]
            static IEnumerator LoadContent(ProjectilePrefabAssetCollection projectilePrefabs, LocalPrefabAssetCollection localPrefabs)
            {
                AsyncOperationHandle<GameObject> commandoGrenadeGhostLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/CommandoGrenadeGhost.prefab");
                while (!commandoGrenadeGhostLoad.IsDone)
                {
                    yield return null;
                }

                if (commandoGrenadeGhostLoad.Status != AsyncOperationStatus.Succeeded)
                {
                    Log.Error($"Failed to load commando grenade ghost prefab: {commandoGrenadeGhostLoad.OperationException}");
                    yield break;
                }

                GameObject grenadeReplacedGhost = commandoGrenadeGhostLoad.Result.InstantiatePrefab("GrenadeReplacedGhost");
                {
                    ProjectileGhostTeamProvider teamProvider = grenadeReplacedGhost.AddComponent<ProjectileGhostTeamProvider>();

                    Transform trailRendererTransform = grenadeReplacedGhost.transform.Find("Helix/GameObject (1)");
                    if (trailRendererTransform)
                    {
                        if (trailRendererTransform.TryGetComponent(out TrailRenderer trailRenderer))
                        {
                            ProjectileGhostTeamIndicator trailTeamIndicator = trailRendererTransform.gameObject.AddComponent<ProjectileGhostTeamIndicator>();
                            trailTeamIndicator.TeamProvider = teamProvider;

                            Material friendlyTrailMaterial = trailRenderer.sharedMaterial;
                            Material enemyTrailMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressFlurryArrowCritTrail.mat").WaitForCompletion();

                            trailTeamIndicator.TeamConfigurations = [
                                new ProjectileGhostTeamIndicator.RenderTeamConfiguration(trailRenderer, [
                                    new ProjectileGhostTeamIndicator.TeamMaterialPair(TeamIndex.Neutral, friendlyTrailMaterial),
                                    new ProjectileGhostTeamIndicator.TeamMaterialPair(TeamIndex.Player, friendlyTrailMaterial),
                                    new ProjectileGhostTeamIndicator.TeamMaterialPair(TeamIndex.Monster, enemyTrailMaterial),
                                    new ProjectileGhostTeamIndicator.TeamMaterialPair(TeamIndex.Lunar, enemyTrailMaterial),
                                    new ProjectileGhostTeamIndicator.TeamMaterialPair(TeamIndex.Void, enemyTrailMaterial)
                                ])
                            ];
                        }
                        else
                        {
                            Log.Error($"Grenade trail {trailRendererTransform} is missing TrailRenderer component");
                        }
                    }
                    else
                    {
                        Log.Error("Failed to find grenade trail renderer");
                    }

                    localPrefabs.Add(grenadeReplacedGhost);
                }

                AsyncOperationHandle<GameObject> commandoGrenadeProjectileLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/CommandoGrenadeProjectile.prefab");
                commandoGrenadeProjectileLoad.OnSuccess(commandoGrenadeProjectile =>
                {
                    GameObject grenadeReplacedProjectile = commandoGrenadeProjectile.InstantiateNetworkedPrefab(nameof(GrenadeReplacedProjectile));

                    if (grenadeReplacedProjectile.TryGetComponent(out ProjectileController projectileController))
                    {
                        projectileController.ghostPrefab = grenadeReplacedGhost;
                        projectileController.startSound = "Play_commando_M2_grenade_throw";
                    }
                    else
                    {
                        Log.Error($"{grenadeReplacedProjectile} is missing ProjectileController component");
                    }

                    grenadeReplacedProjectile.AddComponent<SetProjectileGhostTeam>();

                    projectilePrefabs.Add(grenadeReplacedProjectile);
                });

                yield return commandoGrenadeProjectileLoad;
            }
        }
    }
}
