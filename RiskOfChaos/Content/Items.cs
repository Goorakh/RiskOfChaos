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

        static Items()
        {
            // InvincibleLemurianMarker
            {
                InvincibleLemurianMarker = ScriptableObject.CreateInstance<ItemDef>();
                InvincibleLemurianMarker.name = nameof(InvincibleLemurianMarker);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
#pragma warning disable CS0618 // Type or member is obsolete
                // Setting the tier property here will not work because the ItemTierCatalog is not yet initialized
                InvincibleLemurianMarker.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

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

                    if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<BullseyeSearch>(nameof(BullseyeSearch.GetResults))))
                    {
                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitDelegate((IEnumerable<HurtBox> results, BaseAI instance) =>
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
                        });
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
                        nameToken = "INVINCIBLE_LEMURIAN_BODY_NAME";
                    }
                };

                CharacterTokenOverridePatch.OverrideDisplayName += static (CharacterBody body, ref string displayName) =>
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
                            displayName = Language.GetStringFormatted("INVINCIBLE_LEMURIAN_ELDER_BODY_NAME_FORMAT", displayName);
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
            };
        }

        internal static void AddItemDefsTo(NamedAssetCollection<ItemDef> itemCollection)
        {
            itemCollection.Add(new ItemDef[]
            {
                InvincibleLemurianMarker
            });
        }
    }
}
