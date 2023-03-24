using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect("rotate_gravity", DefaultSelectionWeight = 0.8f, EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class RotateGravity : GenericGravityEffect
    {
        static bool _hasAppliedPatches = false;

        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            IL.RoR2.CharacterMotor.PreMove += il =>
            {
                ILCursor c = new ILCursor(il);

                ILCursor[] foundCursors;
                if (c.TryFindNext(out foundCursors,
                                  x => x.MatchLdarg(0),
                                  x => x.MatchCall(AccessTools.DeclaredPropertyGetter(typeof(CharacterMotor), nameof(CharacterMotor.useGravity))),
                                  x => x.MatchBrfalse(out _)))
                {
                    ILCursor cursor = foundCursors[2];
                    cursor.Index++;

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.EmitDelegate((CharacterMotor instance, float deltaTime) =>
                    {
                        if (!AnyGravityChangeActive)
                            return;

                        Vector3 xzGravity = new Vector3(Physics.gravity.x, 0f, Physics.gravity.z);
                        instance.velocity += xzGravity * deltaTime;
                    });
                }
            };

            IL.RoR2.ModelLocator.UpdateTargetNormal += il =>
            {
                ILCursor c = new ILCursor(il);

                while (c.TryGotoNext(MoveType.After,
                                     x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Vector3), nameof(Vector3.up)))))
                {
                    c.EmitDelegate((Vector3 up) =>
                    {
                        if (AnyGravityChangeActive)
                        {
                            return -Physics.gravity.normalized;
                        }
                        else
                        {
                            return up;
                        }
                    });
                }
            };

            _hasAppliedPatches = true;
        }

        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _maxDeviationConfig;
        const float MAX_DEVITATION_DEFAULT_VALUE = 30f;

        const float MAX_DEVITATION_MIN_VALUE = 0f;
        const float MAX_DEVITATION_MAX_VALUE = 90f;

        static float maxDeviation
        {
            get
            {
                if (_maxDeviationConfig == null)
                {
                    return MAX_DEVITATION_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Clamp(_maxDeviationConfig.Value, MAX_DEVITATION_MIN_VALUE, MAX_DEVITATION_MAX_VALUE);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _maxDeviationConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Max Rotation Angle"), MAX_DEVITATION_DEFAULT_VALUE, new ConfigDescription("The maximum amount of deviation (in degrees) that can be applied to the gravity direction"));

            addConfigOption(new StepSliderOption(_maxDeviationConfig, new StepSliderConfig
            {
                formatString = "{0:F1}",
                min = MAX_DEVITATION_MIN_VALUE,
                max = MAX_DEVITATION_MAX_VALUE,
                increment = 0.5f
            }));
        }

        Quaternion _gravityRotation;

        protected override Vector3 modifyGravity(Vector3 originalGravity)
        {
            return _gravityRotation * originalGravity;
        }

        public override void OnStart()
        {
            tryApplyPatches();

            float maxDeviation = RotateGravity.maxDeviation;
            _gravityRotation = Quaternion.Euler(RNG.RangeFloat(-maxDeviation, maxDeviation),
                                                RNG.RangeFloat(-maxDeviation, maxDeviation),
                                                RNG.RangeFloat(-maxDeviation, maxDeviation));

            base.OnStart();
        }
    }
}
