using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.CharacterAI;
using RoR2.ContentManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Content
{
    public static class Items
    {
        public static readonly ItemDef InvincibleLemurianMarker;

        public static readonly ItemDef MinAllyRegen;

        static Items()
        {
            // InvincibleLemurianMarker
            {
                InvincibleLemurianMarker = ScriptableObject.CreateInstance<ItemDef>();
                InvincibleLemurianMarker.name = nameof(InvincibleLemurianMarker);

#pragma warning disable CS0618 // Type or member is obsolete
                // Setting the tier property here will not work because the ItemTierCatalog is not yet initialized
                InvincibleLemurianMarker.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618 // Type or member is obsolete

                InvincibleLemurianMarker.hidden = true;
                InvincibleLemurianMarker.canRemove = false;

                InvincibleLemurianMarker.AutoPopulateTokens();

                On.RoR2.HealthComponent.TakeDamage += static (orig, self, damageInfo) =>
                {
                    if (NetworkServer.active &&
                        damageInfo != null &&
                        damageInfo.attacker &&
                        damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) &&
                        attackerBody.inventory &&
                        attackerBody.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                    {
                        // Instantly die no matter what
                        damageInfo.damageType |= DamageType.BypassArmor | DamageType.BypassBlock | DamageType.BypassOneShotProtection;
                        damageInfo.damage = float.PositiveInfinity;
                    }

                    orig(self, damageInfo);
                };

                On.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += static (orig, self, maxDistance, full360Vision, filterByLoS) =>
                {
                    if (self && self.master.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                    {
                        maxDistance = float.PositiveInfinity;
                        filterByLoS = false;
                        full360Vision = true;
                    }

                    return orig(self, maxDistance, full360Vision, filterByLoS);
                };

                IL.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += static il =>
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
                };

                On.RoR2.Projectile.ProjectileController.Start += static (orig, self) =>
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
                };

                CharacterTokenOverridePatch.OverrideNameToken += static (CharacterBody body, ref string nameToken) =>
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
                };

                GlobalEventManager.onCharacterDeathGlobal += static (report) =>
                {
                    if (!NetworkServer.active)
                        return;

                    if (!report.victimMaster || report.victimMaster.IsExtraLifePendingServer())
                        return;

                    if (report.victimMaster.inventory.GetItemCount(InvincibleLemurianMarker) <= 0)
                        return;

                    if (report.victimTeamIndex == TeamIndex.Player)
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
                };

                CustomPlayerDeathMessageTokenPatch.OverridePlayerDeathMessageToken += static (DamageReport damageReport, ref string messageToken) =>
                {
                    if (damageReport is null || !damageReport.attackerMaster)
                        return;

                    if (damageReport.attackerMaster.inventory.GetItemCount(InvincibleLemurianMarker) > 0)
                    {
                        messageToken = "PLAYER_DEATH_QUOTE_INVINCIBLE_LEMURIAN";
                    }
                };
            }

            // MinAllyRegen
            {
                MinAllyRegen = ScriptableObject.CreateInstance<ItemDef>();
                MinAllyRegen.name = nameof(MinAllyRegen);

#pragma warning disable CS0618 // Type or member is obsolete
                // Setting the tier property here will not work because the ItemTierCatalog is not yet initialized
                MinAllyRegen.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618 // Type or member is obsolete

                MinAllyRegen.hidden = true;
                MinAllyRegen.canRemove = false;

                MinAllyRegen.AutoPopulateTokens();
            }

            RecalculateStatsAPI.GetStatCoefficients += static (body, args) =>
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
                    const float TARGT_BASE_REGEN = 2.5f;
                    if (body.baseRegen < TARGT_BASE_REGEN)
                    {
                        args.baseRegenAdd += TARGT_BASE_REGEN - body.baseRegen;
                    }

                    const float TARGET_LEVEL_REGEN = 0.5f;
                    if (body.levelRegen < TARGET_LEVEL_REGEN)
                    {
                        args.levelRegenAdd += TARGET_LEVEL_REGEN - body.levelRegen;
                    }
                }
            };
        }

        internal static void AddItemDefsTo(NamedAssetCollection<ItemDef> itemCollection)
        {
            itemCollection.Add([
                InvincibleLemurianMarker,
                MinAllyRegen,
            ]);
        }
    }
}
