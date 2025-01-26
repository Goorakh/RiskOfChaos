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
                static IEnumerator loadReplacedGrenade(ProjectilePrefabAssetCollection projectilePrefabs, LocalPrefabAssetCollection localPrefabs)
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

                        grenadeReplacedProjectile.AddComponent<ReplacedProjectileHookReferenceResolver>();

                        projectilePrefabs.Add(grenadeReplacedProjectile);
                    });

                    yield return commandoGrenadeProjectileLoad;
                }

                yield return loadReplacedGrenade(projectilePrefabs, localPrefabs);

                static IEnumerator loadPulseGolemHookProjectile(ProjectilePrefabAssetCollection projectilePrefabs, LocalPrefabAssetCollection localPrefabs)
                {
                    AsyncOperationHandle<GameObject> hookProjectileLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Gravekeeper/GravekeeperHookProjectileSimple.prefab");
                    AsyncOperationHandle<GameObject> hookGhostLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Gravekeeper/GravekeeperHookGhost.prefab");

                    AsyncOperationHandle[] loadOperations = [hookProjectileLoad, hookGhostLoad];
                    yield return loadOperations.WaitForAllLoaded();

                    if (hookProjectileLoad.Status != AsyncOperationStatus.Succeeded)
                    {
                        Log.Error($"Failed to load hook projectile prefab: {hookProjectileLoad.OperationException}");
                        yield break;
                    }

                    if (hookGhostLoad.Status != AsyncOperationStatus.Succeeded)
                    {
                        Log.Error($"Failed to load hook ghost prefab: {hookGhostLoad.OperationException}");
                        yield break;
                    }

                    GameObject hookProjectileGhostPrefab = hookGhostLoad.Result.InstantiatePrefab("PulseGolemHookProjectileGhost");

                    localPrefabs.Add(hookProjectileGhostPrefab);

                    GameObject hookProjectilePrefab = hookProjectileLoad.Result.InstantiateNetworkedPrefab(nameof(PulseGolemHookProjectile));

                    ProjectileController hookProjectileController = hookProjectilePrefab.GetComponent<ProjectileController>();
                    hookProjectileController.ghostPrefab = hookProjectileGhostPrefab;

                    ProjectileSimple hookProjectileSimple = hookProjectilePrefab.GetComponent<ProjectileSimple>();
                    hookProjectileSimple.desiredForwardSpeed = 250f;

                    ProjectileStickOnImpact hookProjectileStickOnImpact = hookProjectilePrefab.GetComponent<ProjectileStickOnImpact>();
                    hookProjectileStickOnImpact.ignoreCharacters = false;
                    hookProjectileStickOnImpact.ignoreWorld = false;
                    hookProjectileStickOnImpact.alignNormals = false;

                    ProjectileHookController hookProjectileHookController = hookProjectilePrefab.AddComponent<ProjectileHookController>();
                    hookProjectileHookController.ReelDelay = 0.1f;
                    hookProjectileHookController.ReelSpeed = 100f;
                    hookProjectileHookController.PullOriginChildName = "MuzzleLaser";
                    hookProjectileHookController.PullTargetDistance = 15f;
                    hookProjectileHookController.HookCharacterEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/OmniImpactVFXSlash.prefab").WaitForCompletion();

                    GameObject.Destroy(hookProjectilePrefab.GetComponent<ProjectileSingleTargetImpact>());

                    projectilePrefabs.Add(hookProjectilePrefab);
                }

                yield return loadPulseGolemHookProjectile(projectilePrefabs, localPrefabs);
            }
        }
    }
}
