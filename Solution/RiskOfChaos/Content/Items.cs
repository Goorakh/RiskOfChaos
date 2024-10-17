using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Content
{
    partial class RoCContent
    {
        partial class Items
        {
            [ContentInitializer]
            static void LoadContent(ItemDefAssetCollection items)
            {
                // InvincibleLemurianMarker
                {
                    ItemDef invincibleLemurianMarker = ScriptableObject.CreateInstance<ItemDef>();
                    invincibleLemurianMarker.name = nameof(InvincibleLemurianMarker);

#pragma warning disable CS0618 // Type or member is obsolete
                    // Setting the tier property here will not work because the ItemTierCatalog is not yet initialized
                    invincibleLemurianMarker.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618 // Type or member is obsolete

                    invincibleLemurianMarker.hidden = true;
                    invincibleLemurianMarker.canRemove = false;

                    invincibleLemurianMarker.AutoPopulateTokens();

                    items.Add(invincibleLemurianMarker);
                }

                // MinAllyRegen
                {
                    ItemDef minAllyRegen = ScriptableObject.CreateInstance<ItemDef>();
                    minAllyRegen.name = nameof(MinAllyRegen);

#pragma warning disable CS0618 // Type or member is obsolete
                    // Setting the tier property here will not work because the ItemTierCatalog is not yet initialized
                    minAllyRegen.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618 // Type or member is obsolete

                    minAllyRegen.hidden = true;
                    minAllyRegen.canRemove = false;

                    minAllyRegen.AutoPopulateTokens();

                    items.Add(minAllyRegen);
                }
            }

            [SystemInitializer]
            static void InitHooks()
            {
                On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;

                On.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += On_BaseAI_FindEnemyHurtBox;

                IL.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += IL_BaseAI_FindEnemyHurtBox;

                On.RoR2.Projectile.ProjectileController.Start += ProjectileController_Start;

                CharacterTokenOverridePatch.OverrideNameToken += CharacterTokenOverridePatch_OverrideNameToken;

                GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

                CustomPlayerDeathMessageTokenPatch.OverridePlayerDeathMessageToken += CustomPlayerDeathMessageTokenPatch_OverridePlayerDeathMessageToken;

                RecalculateStatsAPI.GetStatCoefficients += getStatCoefficients;
            }

            static void getStatCoefficients(CharacterBody body, RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (!body)
                    return;

                Inventory inventory = body.inventory;
                if (!inventory)
                    return;

                if (inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                {
                    args.attackSpeedReductionMultAdd += 0.5f;
                    args.moveSpeedReductionMultAdd += 1f;
                }

                if (inventory.GetItemCount(MinAllyRegen) > 0)
                {
                    const float TARGET_BASE_REGEN = 2.5f;
                    if (body.baseRegen < TARGET_BASE_REGEN)
                    {
                        args.baseRegenAdd += TARGET_BASE_REGEN - body.baseRegen;
                    }

                    const float TARGET_LEVEL_REGEN = 0.5f;
                    if (body.levelRegen < TARGET_LEVEL_REGEN)
                    {
                        args.levelRegenAdd += TARGET_LEVEL_REGEN - body.levelRegen;
                    }
                }
            }

            static void GlobalEventManager_onCharacterDeathGlobal(DamageReport damageReport)
            {
                if (!NetworkServer.active)
                    return;

                if (!damageReport.victimMaster || damageReport.victimMaster.IsExtraLifePendingServer())
                    return;

                if (damageReport.victimMaster.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                {
                    if (damageReport.victimTeamIndex == TeamIndex.Player)
                    {
                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                        {
                            baseToken = "INVINCIBLE_LEMURIAN_DEATH_ALLY_MESSAGE"
                        });
                    }
                    else
                    {
                        // The promised million dollars
                        PlayerUtils.GetAllPlayerMasters(false).TryDo(m => m.GiveMoney(1_000_000), Util.GetBestMasterName);

                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                        {
                            baseToken = "INVINCIBLE_LEMURIAN_DEATH_REWARD_MESSAGE"
                        });
                    }
                }
            }

            static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
            {
                if (NetworkServer.active &&
                    damageInfo != null &&
                    damageInfo.attacker &&
                    damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) &&
                    attackerBody.inventory &&
                    attackerBody.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                {
                    // Instantly die no matter what
                    damageInfo.damageType |= new DamageTypeCombo(DamageType.BypassArmor | DamageType.BypassBlock | DamageType.BypassOneShotProtection, DamageTypeExtended.SojournVehicleDamage);
                    damageInfo.damage = float.PositiveInfinity;
                }

                orig(self, damageInfo);
            }

            static HurtBox On_BaseAI_FindEnemyHurtBox(On.RoR2.CharacterAI.BaseAI.orig_FindEnemyHurtBox orig, BaseAI self, float maxDistance, bool full360Vision, bool filterByLoS)
            {
                if (self && self.master.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                {
                    maxDistance = float.PositiveInfinity;
                    filterByLoS = false;
                    full360Vision = true;
                }

                return orig(self, maxDistance, full360Vision, filterByLoS);
            }

            static void IL_BaseAI_FindEnemyHurtBox(ILContext il)
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchCallOrCallvirt<BullseyeSearch>(nameof(BullseyeSearch.GetResults))))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(filterTargets);
                    static IEnumerable<HurtBox> filterTargets(IEnumerable<HurtBox> results, BaseAI instance)
                    {
                        if (instance && instance.master.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                        {
                            // Filter results to only target players (don't target player allies like drones)
                            IEnumerable<HurtBox> playerControlledTargets = results.Where(hurtBox =>
                            {
                                GameObject entityObject = HurtBox.FindEntityObject(hurtBox);
                                return entityObject && entityObject.TryGetComponent(out CharacterBody characterBody) && characterBody.isPlayerControlled;
                            });

                            // If there are no players, use the default target so that the AI doesn't end up doing nothing
                            return playerControlledTargets.Any() ? playerControlledTargets : results;
                        }
                        else
                        {
                            return results;
                        }
                    }
                }
                else
                {
                    Log.Error("Failed to find InvincibleLemurianMarker ai targets override patch location");
                }
            }

            static void ProjectileController_Start(On.RoR2.Projectile.ProjectileController.orig_Start orig, ProjectileController self)
            {
                orig(self);

                if (!self.owner || !self.owner.TryGetComponent(out CharacterBody ownerBody))
                    return;

                Inventory ownerInventory = ownerBody.inventory;
                if (!ownerInventory)
                    return;

                if (ownerInventory.GetItemCount(InvincibleLemurianMarker) > 0)
                {
                    self.cannotBeDeleted = true;
                }
            }

            static void CharacterTokenOverridePatch_OverrideNameToken(CharacterBody body, ref string nameToken)
            {
                if (!body)
                    return;

                Inventory inventory = body.inventory;
                if (!inventory)
                    return;

                if (inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                {
                    if (body.bodyIndex == BodyCatalog.FindBodyIndex("LemurianBruiserBody"))
                    {
                        nameToken = "INVINCIBLE_LEMURIAN_ELDER_BODY_NAME";
                    }
                    else
                    {
                        nameToken = "INVINCIBLE_LEMURIAN_BODY_NAME";
                    }
                }
            }

            static void CustomPlayerDeathMessageTokenPatch_OverridePlayerDeathMessageToken(DamageReport damageReport, ref string messageToken)
            {
                if (damageReport is null || !damageReport.attackerMaster)
                    return;

                if (damageReport.attackerMaster.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                {
                    messageToken = "PLAYER_DEATH_QUOTE_INVINCIBLE_LEMURIAN";
                }
            }
        }
    }
}
