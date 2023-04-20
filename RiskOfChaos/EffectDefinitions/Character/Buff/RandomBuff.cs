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

#if DEBUG
                Log.Debug(il);
#endif
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
                if (!buffDef || buffDef.isHidden || isDebuff(buffDef) || buffDef.isCooldown)
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

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();
            tryApplyPatches();
        }

        static int _debugIndex = 0;

        protected override BuffIndex getBuffIndexToApply()
        {
            BuffIndex selectedBuff = RNG.NextElementUniform(filterSelectableBuffs(_availableBuffIndices).ToList());
            //selectedBuff = _availableBuffIndices[_debugIndex++ % _availableBuffIndices.Length];

#if DEBUG
            BuffDef buffDef = BuffCatalog.GetBuffDef(selectedBuff);
            Log.Debug($"Applying buff {buffDef?.name ?? "null"}");
#endif

            return selectedBuff;
        }
    }
}
