using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
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

                switch (buffDef.name)
                {
                    case "bdBearVoidReady": // Invincibility
                    case "bdBodyArmor": // Invincibility
                    case "bdCloak": // Invisibility not fun
                    case "bdCrocoRegen": // Too much regen
                    case "bdElementalRingsReady": // Doesn't work without the item
                    case "bdElementalRingVoidReady": // Doesn't work without the item
                    case "bdEliteSecretSpeed": // Does nothing
                    case "bdEliteHauntedRecipient": // Invisibility not fun
                    case "bdGoldEmpowered": // Invincibility
                    case "bdHiddenInvincibility": // Invincibility
                    case "bdImmune": // Invincibility
                    case "bdImmuneToDebuffReady": // Does nothing without the item
                    case "bdIntangible": // Invincibility
                    case "bdLaserTurbineKillCharge": // Doesn't work without the item
                    case "bdLightningShield": // Does nothing
                    case "bdLoaderOvercharged": // Does nothing unless you are Loader
                    case "bdMedkitHeal": // Doesn't do anything if constantly applied
                    case "bdMushroomVoidActive": // Does nothing without the item
                    case "bdOutOfCombatArmorBuff": // Does nothing without the item
                    case "bdPrimarySkillShurikenBuff": // Does nothing without the item
                    case "bdTeslaField": // Doesn't work without the item
                    case "bdVoidFogMild": // Does nothing
                    case "bdVoidFogStrong": // Does nothing
                    case "bdVoidRaidCrabWardWipeFog": // Does nothing
                    case "bdVoidSurvivorCorruptMode": // Does nothing
                    case "bdWhipBoost": // Doesn't work without the item
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: blacklist");
#endif
                        return false;

                    #region MysticsItems compat
                    case "MysticsItems_BuffInTPRange": // Doesn't work without item
                    case "MysticsItems_DasherDiscActive": // Invincibility
                    case "MysticsItems_GachaponBonus": // Doesn't work without item
                    case "MysticsItems_MechanicalArmCharge": // Does nothing
                    case "MysticsItems_NanomachineArmor": // Doesn't work without item
                    case "MysticsItems_StarPickup": // Doesn't work without item
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: MysticsItems compat blacklist");
#endif
                        return false;
                    #endregion

                    #region TsunamiItemsRevived compat
                    case "ColaBoostBuff": // Doesn't work without item
                    case "GeigerBuff": // Does nothing
                    case "ManualReadyBuff": // Doesn't work without item
                    case "SandwichHealBuff": // Doesn't work without item
                    case "SkullBuff": // Does nothing
                    case "SuppressorBoostBuff": // Doesn't work without item
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: TsunamiItemsRevived compat blacklist");
#endif
                        return false;
                    #endregion

                    #region ExtradimensionalItems compat
                    case "Adrenaline Protection": // Doesn't work without item
                    case "Damage On Cooldowns": // Doesn't work without item
                    case "Royal Guard Damage Buff": // Doesn't work without item
                    case "Royal Guard Parry State": // Doesn't work without item
                    case "Sheen Damage Bonus": // Doesn't work without item
                    case "Skull of Impending Doom": // Doesn't work without item
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: ExtradimensionalItems compat blacklist");
#endif
                        return false;
                    #endregion

                    #region VanillaVoid compat
                    case "ZnVVOrreryDamage": // Doesn't work without item
                    case "ZnVVshatterStatus": // Doesn't work without item
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: VanillaVoid compat blacklist");
#endif
                        return false;
                    #endregion

                    #region SpireItems compat
                    case "Buffer": // Invincibility
                    case "Mantra": // Does nothing
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: SpireItems compat blacklist");
#endif
                        return false;
                    #endregion

                    #region LostInTransit compat
                    case "GoldenGun": // Doesn't work without item
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: LostInTransit compat blacklist");
#endif
                        return false;
                    #endregion

                    #region Starstorm2 compat
                    case "bdElitePurple": // Does nothing
                    case "BuffChirrAlly": // Does nothing
                    case "BuffExecutionerSuperCharged": // Does nothing unless you are Executioner
                    case "BuffKickflip": // Doesn't work without item
                    case "BuffReactor": // Invincibility
                    case "BuffTerminationFailed": // Does nothing
                    case "BuffTerminationReady": // Doesn't work without item
                    case "BuffTerminationVFX": // Does nothing
#if DEBUG
                        Log.Debug($"Excluding buff {buffDef.name}: Starstorm2 compat blacklist");
#endif
                        return false;
                    #endregion
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
