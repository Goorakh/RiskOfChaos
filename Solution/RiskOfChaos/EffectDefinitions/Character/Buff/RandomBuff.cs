using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosTimedEffect("random_buff", 90f)]
    [EffectConfigBackwardsCompatibility("Effect: Give Everyone a Random Buff (Lasts 1 stage)")]
    [RequiredComponents(typeof(ApplyBuffEffect))]
    public sealed class RandomBuff : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _stackableBuffCount =
            ConfigFactory<int>.CreateConfig("Buff Stack Count", 5)
                              .Description("How many stacks of the buff should be given, if the random buff is stackable")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        static readonly BuffIndexCollection _buffBlacklist = new BuffIndexCollection([
            "bdAurelioniteBlessing", // Nullref spam
            "bdBearVoidReady", // Invincibility
            "bdBodyArmor", // Invincibility
            "bdBoostAllStatsBuff", // Doesn't work without the item
            "bdBoosted", // Does nothing unless you are Chef
            "bdChakraBuff", // Does nothing unless you are Seeker
            "bdCloak", // Invisibility not fun
            "bdCrocoRegen", // Too much regen
            "bdElementalRingsReady", // Doesn't work without the item
            "bdElementalRingVoidReady", // Doesn't work without the item
            "bdEliteSecretSpeed", // Does nothing
            "bdEliteHauntedRecipient", // Invisibility not fun
            "bdExtraLifeBuff", // Basically invincibility
            "bdExtraStatsOnLevelUpBuff", // Doesn't work without the item
            "bdGoldEmpowered", // Invincibility
            "bdHiddenInvincibility", // Invincibility
            "bdImmune", // Invincibility
            "bdImmuneToDebuffReady", // Does nothing without the item
            "bdIncreasePrimaryDamageBuff", // Doesn't work without the item
            "bdIntangible", // Invincibility
            "bdKnockDownHitEnemies", // Does nothing
            "bdLaserTurbineKillCharge", // Doesn't work without the item
            "bdLightningShield", // Does nothing
            "bdLoaderOvercharged", // Does nothing unless you are Loader
            "bdLowerHealthHigherDamageBuff", // Doesn't work without the item
            "bdMedkitHeal", // Doesn't do anything if constantly applied
            "bdMushroomVoidActive", // Does nothing without the item
            "bdOutOfCombatArmorBuff", // Does nothing without the item
            "bdPrimarySkillShurikenBuff", // Does nothing without the item
            "bdStunAndPierceBuff", // Does nothing
            "bdTeslaField", // Doesn't work without the item
            "bdVoidFogMild", // Does nothing
            "bdVoidFogStrong", // Does nothing
            "bdVoidRaidCrabWardWipeFog", // Does nothing
            "bdVoidSurvivorCorruptMode", // Does nothing
            "bdWhipBoost", // Doesn't work without the item

            // Patchable
            "bdDelayedDamageBuff", // Removed if item is missing, otherwise works
            "bdIncreaseDamageBuff", // Removed if multikill missing, otherwise works
            "bdTeleportOnLowHealth", // Only triggers if item is present

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

            #region WolfoQoL compat
            "visual_HelFire",
            "visual_EnemyBurn",
            "visual_BugFlight",
            "visual_ShadowIntangible",
            "visual_SprintArmor",
            "visual_Frozen",
            "visual_HeadstomperReady",
            "visual_HeadstomperCooldown",
            "visual_BonusJump",
            "visual_ShieldDelay",
            "visual_OutOfCombatArmorCooldown",
            "visual_VolcanoEgg",
            "visual_TinctureIgnition",
            "visual_FrostRelicGrowth",
            "visual_FakeShurikenStock",
            "visual_ImpendingVagrantExplosion"
	        #endregion
        ]);

        static BuffIndex[] _availableBuffIndices = [];

        [SystemInitializer(typeof(BuffCatalog), typeof(DotController))]
        static void InitAvailableBuffs()
        {
            _availableBuffIndices = Enumerable.Range(0, BuffCatalog.buffCount).Select(i => (BuffIndex)i).Where(bi =>
            {
                if (bi == BuffIndex.None)
                    return false;

                BuffDef buffDef = BuffCatalog.GetBuffDef(bi);
                if (!buffDef || buffDef.isHidden || BuffUtils.IsDebuff(buffDef) || BuffUtils.IsCooldown(buffDef))
                {
#if DEBUG
                    Log.Debug($"Excluding hidden/debuff/cooldown buff {buffDef.name}");
#endif
                    return false;
                }

                if (BuffUtils.IsDOT(buffDef))
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
            return _availableBuffIndices.Length > 0 && ApplyBuffEffect.FilterSelectableBuffs(_availableBuffIndices).Any();
        }

        ChaosEffectComponent _chaosEffect;
        ApplyBuffEffect _applyBuffEffect;

        void Awake()
        {
            _chaosEffect = GetComponent<ChaosEffectComponent>();
            _applyBuffEffect = GetComponent<ApplyBuffEffect>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_chaosEffect.Rng.nextUlong);

            BuffIndex buffIndex = getBuffIndexToApply(rng);
            _applyBuffEffect.BuffIndex = buffIndex;

            BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
            _applyBuffEffect.BuffStackCount = buffDef && buffDef.canStack ? _stackableBuffCount.Value : 1;
        }

#if DEBUG
        static int _debugIndex = 0;
        static bool _enableDebugIndex = false;
#endif

        static BuffIndex getBuffIndexToApply(Xoroshiro128Plus rng)
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
                selectedBuff = rng.NextElementUniform(ApplyBuffEffect.FilterSelectableBuffs(_availableBuffIndices).ToList());
            }

#if DEBUG
            BuffDef buffDef = BuffCatalog.GetBuffDef(selectedBuff);
            Log.Debug($"Applying buff {buffDef?.name ?? "null"}");
#endif

            return selectedBuff;
        }
    }
}
