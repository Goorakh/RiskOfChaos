using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.CatalogIndexCollection;
using RoR2;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosEffect("random_buff")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: Give Everyone a Random Buff (Lasts 1 stage)")]
    public sealed class RandomBuff : ApplyBuffEffect
    {
        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            // Buffs that spawn another character on death should be added here to prevent infinite respawning of that character type
            IL.RoR2.GlobalEventManager.OnCharacterDeath += il =>
            {
                ILCursor c = new ILCursor(il);

                int victimBodyLocalIndex = -1;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchLdloc(out victimBodyLocalIndex),
                                  x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.AffixPoison)),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<CharacterBody>(_ => _.HasBuff(default(BuffDef))))))
                {
                    c.Emit(OpCodes.Ldloc, victimBodyLocalIndex);
                    c.EmitDelegate((CharacterBody victimBody) =>
                    {
                        return victimBody && victimBody.bodyIndex != BodyCatalog.FindBodyIndex("UrchinTurretBody");
                    });
                    c.Emit(OpCodes.And);
                }
                else
                {
                    Log.Warning("Failed to find malachite urchin patch location");
                }

                c.Index = 0;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchLdloc(out victimBodyLocalIndex),
                                  x => x.MatchLdsfld(typeof(DLC1Content.Buffs), nameof(DLC1Content.Buffs.EliteEarth)),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<CharacterBody>(_ => _.HasBuff(default(BuffDef))))))
                {
                    c.Emit(OpCodes.Ldloc, victimBodyLocalIndex);
                    c.EmitDelegate((CharacterBody victimBody) =>
                    {
                        return victimBody && victimBody.bodyIndex != BodyCatalog.FindBodyIndex("AffixEarthHealerBody");
                    });
                    c.Emit(OpCodes.And);
                }
                else
                {
                    Log.Warning("Failed to find healing core patch location");
                }

                c.Index = 0;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchLdloc(out victimBodyLocalIndex),
                                  x => x.MatchLdsfld(typeof(DLC1Content.Buffs), nameof(DLC1Content.Buffs.EliteVoid)),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<CharacterBody>(_ => _.HasBuff(default(BuffDef))))))
                {
                    c.Emit(OpCodes.Ldloc, victimBodyLocalIndex);
                    c.EmitDelegate((CharacterBody victimBody) =>
                    {
                        return victimBody && victimBody.bodyIndex != BodyCatalog.FindBodyIndex("VoidInfestorBody");
                    });
                    c.Emit(OpCodes.And);
                }
                else
                {
                    Log.Warning("Failed to find void infestor patch location");
                }
            };

            _hasAppliedPatches = true;
        }

        static readonly BuffIndexCollection _buffBlacklist = new BuffIndexCollection(new string[]
        {
            "bdBearVoidReady", // Invincibility
            "bdBodyArmor", // Invincibility
            "bdCloak", // Invisibility not fun
            "bdCrocoRegen", // Too much regen
            "bdElementalRingsReady", // Doesn't work without the item
            "bdElementalRingVoidReady", // Doesn't work without the item
            "bdEliteSecretSpeed", // Does nothing
            "bdEliteHauntedRecipient", // Invisibility not fun
            "bdGoldEmpowered", // Invincibility
            "bdHiddenInvincibility", // Invincibility
            "bdImmune", // Invincibility
            "bdImmuneToDebuffReady", // Does nothing without the item
            "bdIntangible", // Invincibility
            "bdLaserTurbineKillCharge", // Doesn't work without the item
            "bdLightningShield", // Does nothing
            "bdLoaderOvercharged", // Does nothing unless you are Loader
            "bdMedkitHeal", // Doesn't do anything if constantly applied
            "bdMushroomVoidActive", // Does nothing without the item
            "bdOutOfCombatArmorBuff", // Does nothing without the item
            "bdPrimarySkillShurikenBuff", // Does nothing without the item
            "bdTeslaField", // Doesn't work without the item
            "bdVoidFogMild", // Does nothing
            "bdVoidFogStrong", // Does nothing
            "bdVoidRaidCrabWardWipeFog", // Does nothing
            "bdVoidSurvivorCorruptMode", // Does nothing
            "bdWhipBoost", // Doesn't work without the item

            #region MysticsItems compat
            "MysticsItems_BuffInTPRange", // Doesn't work without item
            "MysticsItems_DasherDiscActive", // Invincibility
            "MysticsItems_GachaponBonus", // Doesn't work without item
            "MysticsItems_MechanicalArmCharge", // Does nothing
            "MysticsItems_NanomachineArmor", // Doesn't work without item
            "MysticsItems_StarPickup", // Doesn't work without item

            #endregion

            #region TsunamiItemsRevived compat
            "ColaBoostBuff", // Doesn't work without item
            "GeigerBuff", // Does nothing
            "ManualReadyBuff", // Doesn't work without item
            "SandwichHealBuff", // Doesn't work without item
            "SkullBuff", // Does nothing
            "SuppressorBoostBuff", // Doesn't work without item
            #endregion

            #region ExtradimensionalItems compat
            "Adrenaline Protection", // Doesn't work without item
            "Damage On Cooldowns", // Doesn't work without item
            "Royal Guard Damage Buff", // Doesn't work without item
            "Royal Guard Parry State", // Doesn't work without item
            "Sheen Damage Bonus", // Doesn't work without item
            "Skull of Impending Doom", // Doesn't work without item
            #endregion

            #region VanillaVoid compat
            "ZnVVOrreryDamage", // Doesn't work without item
            "ZnVVshatterStatus", // Doesn't work without item
            #endregion

            #region SpireItems compat
            "Buffer", // Invincibility
            "Mantra", // Does nothing
            #endregion

            #region LostInTransit compat
            "GoldenGun", // Doesn't work without item
            #endregion

            #region Starstorm2 compat
            "bdElitePurple", // Does nothing
            "BuffChirrAlly", // Does nothing
            "BuffExecutionerSuperCharged", // Does nothing unless you are Executioner
            "BuffKickflip", // Doesn't work without item
            "BuffReactor", // Invincibility
            "BuffTerminationFailed", // Does nothing
            "BuffTerminationReady", // Doesn't work without item
            "BuffTerminationVFX", // Does nothing
            #endregion
        });

        static BuffIndex[] _availableBuffIndices;

        [SystemInitializer(typeof(BuffCatalog), typeof(DotController))]
        static void InitAvailableBuffs()
        {
            _availableBuffIndices = Enumerable.Range(0, BuffCatalog.buffCount).Select(i => (BuffIndex)i).Where(bi =>
            {
                if (bi == BuffIndex.None)
                    return false;

                BuffDef buffDef = BuffCatalog.GetBuffDef(bi);
                if (!buffDef || buffDef.isHidden || isDebuff(buffDef) || isCooldown(buffDef))
                {
#if DEBUG
                    Log.Debug($"Excluding hidden/debuff/cooldown buff {buffDef.name}");
#endif
                    return false;
                }

                if (isDOT(buffDef))
                {
#if DEBUG
                    Log.Debug($"Excluding DOT buff: {buffDef.name}");
#endif
                    return false;
                }

                if (_buffBlacklist.Contains(buffDef.buffIndex))
                {
#if DEBUG
                    Log.Debug($"Excluding buff {buffDef.name}: blacklist");
#endif
                    return false;
                }

#if DEBUG
                Log.Debug($"Including buff {buffDef.name}");
#endif

                return true;
            }).ToArray();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _availableBuffIndices != null && filterSelectableBuffs(_availableBuffIndices).Any();
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();
            tryApplyPatches();
        }

#if DEBUG
        static int _debugIndex = 0;
        static bool _enableDebugIndex = false;
#endif

        protected override BuffIndex getBuffIndexToApply()
        {
            BuffIndex selectedBuff;

#if DEBUG
            if (_enableDebugIndex)
            {
                selectedBuff = _availableBuffIndices[_debugIndex++ % _availableBuffIndices.Length];
            }
            else
#endif
            {
                selectedBuff = RNG.NextElementUniform(filterSelectableBuffs(_availableBuffIndices).ToList());
            }

#if DEBUG
            BuffDef buffDef = BuffCatalog.GetBuffDef(selectedBuff);
            Log.Debug($"Applying buff {buffDef?.name ?? "null"}");
#endif

            return selectedBuff;
        }
    }
}
