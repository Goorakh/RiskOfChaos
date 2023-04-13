using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
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

        sealed class InvincibleLemurianItemBehavior : CharacterBody.ItemBehavior
        {
            void FixedUpdate()
            {
                if (!body)
                    return;

                if (!body.HasBuff(RoR2Content.Buffs.Immune))
                {
                    body.AddBuff(RoR2Content.Buffs.Immune);
                }
            }

            void OnDisable()
            {
                if (body)
                {
                    if (body.HasBuff(RoR2Content.Buffs.Immune))
                    {
                        body.RemoveBuff(RoR2Content.Buffs.Immune);
                    }
                }
            }
        }

        static Items()
        {
            // InvincibleLemurianHelper
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
                                return results.Where(hurtBox =>
                                {
                                    GameObject entityObject = HurtBox.FindEntityObject(hurtBox);
                                    return entityObject && entityObject.TryGetComponent(out CharacterBody characterBody) && characterBody.isPlayerControlled;
                                });
                            }
                            else
                            {
                                return results;
                            }
                        });
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
                    args.moveSpeedReductionMultAdd += 1f;
                }
            };

            CharacterBody.onBodyInventoryChangedGlobal += body =>
            {
                if (NetworkServer.active)
                {
                    Inventory inventory = body.inventory;
                    if (inventory)
                    {
                        body.AddItemBehavior<InvincibleLemurianItemBehavior>(inventory.GetItemCount(InvincibleLemurianMarker));
                    }
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
