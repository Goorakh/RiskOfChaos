using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Networking;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("increase_knockback", ConfigName = "Increase Knockback", EffectWeightReductionPercentagePerActivation = 30f, IsNetworked = true)]
    public sealed class IncreaseKnockback : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static float _totalMultiplier = 1f;

        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.CharacterMotor.ApplyForceImpulse += (On.RoR2.CharacterMotor.orig_ApplyForceImpulse orig, CharacterMotor self, ref PhysForceInfo forceInfo) =>
            {
                tryMultiplyForce(self.hasEffectiveAuthority, ref forceInfo);

                orig(self, ref forceInfo);
            };

            On.RoR2.RigidbodyMotor.ApplyForceImpulse += (On.RoR2.RigidbodyMotor.orig_ApplyForceImpulse orig, RigidbodyMotor self, ref PhysForceInfo forceInfo) =>
            {
                tryMultiplyForce(self.hasEffectiveAuthority, ref forceInfo);

                orig(self, ref forceInfo);
            };

            _hasAppliedPatches = true;
        }

        static void tryMultiplyForce(bool hasAuthority, ref PhysForceInfo forceInfo)
        {
            if (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(_effectInfo))
            {
                if (hasAuthority)
                {
                    forceInfo.force *= _totalMultiplier;
                }
                else
                {
#if DEBUG
                    Log.Debug($"Not multiplying force, NetworkServer.active={NetworkServer.active}, {nameof(hasAuthority)}={hasAuthority}");
#endif
                }
            }
        }

        static ConfigEntry<float> _knockbackMultiplierConfig;
        const float KNOCKBACK_MULTIPLIER_DEFAULT_VALUE = 3f;

        const float KNOCKBACK_MULTIPLIER_INCREMENT = 0.1f;
        const float KNOCKBACK_MULTIPLIER_MIN_VALUE = 1f + KNOCKBACK_MULTIPLIER_INCREMENT;

        static float knockbackMultiplier
        {
            get
            {
                if (_knockbackMultiplierConfig == null)
                {
                    return KNOCKBACK_MULTIPLIER_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_knockbackMultiplierConfig.Value, KNOCKBACK_MULTIPLIER_MIN_VALUE);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _knockbackMultiplierConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Knockback Multiplier"), KNOCKBACK_MULTIPLIER_DEFAULT_VALUE, new ConfigDescription("The multiplier used to increase knockback while the effect is active"));

            addConfigOption(new StepSliderOption(_knockbackMultiplierConfig, new StepSliderConfig
            {
                formatString = "{0:F1}x",
                min = KNOCKBACK_MULTIPLIER_MIN_VALUE,
                max = 15f,
                increment = KNOCKBACK_MULTIPLIER_INCREMENT
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { knockbackMultiplier };
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _totalMultiplier *= knockbackMultiplier;
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_totalMultiplier);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _totalMultiplier = reader.ReadSingle();
        }

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        public override void OnStart()
        {
            tryApplyPatches();
        }

        public override void OnEnd()
        {
            _totalMultiplier = 1f;
        }
    }
}
