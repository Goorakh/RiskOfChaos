using BepInEx.Configuration;
using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("enable_random_artifact", EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class EnableRandomArtifact : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        readonly struct ArtifactConfig
        {
            public readonly ConfigEntry<float> SelectionWeight;

            public ArtifactConfig(ArtifactIndex artifactIndex)
            {
                ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(artifactIndex);
                if (!artifactDef)
                {
                    Log.Error($"Invalid artifact index {artifactIndex}");
                    return;
                }

                string artifactName = Language.GetString(artifactDef.nameToken, "en");

                SelectionWeight = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, $"{artifactName.FilterConfigKey()} Weight"), 1f, new ConfigDescription($"How likely the {artifactName} is to be picked, higher value means more likely, lower value means less likely.\n\nA value of 0 will exclude it completely"));
                addConfigOption(new StepSliderOption(SelectionWeight, new StepSliderConfig
                {
                    formatString = "{0:F1}",
                    increment = 0.1f,
                    min = 0f,
                    max = 2.5f
                }));
            }
        }
        static ArtifactConfig[] _artifactConfigs;

        [SystemInitializer(typeof(ArtifactCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _artifactConfigs = new ArtifactConfig[ArtifactCatalog.artifactCount];
            for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                _artifactConfigs[i] = new ArtifactConfig((ArtifactIndex)i);
            }
        }

        static ArtifactConfig? getArtifactConfig(ArtifactIndex index)
        {
            if (!ArrayUtils.IsInBounds(_artifactConfigs, (int)index))
                return null;

            return _artifactConfigs[(int)index];
        }

        static float getArtifactSelectionWeight(ArtifactIndex index)
        {
            ArtifactConfig? config = getArtifactConfig(index);
            if (!config.HasValue)
                return 0f;

            return config.Value.SelectionWeight.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool canEnableArtifact(ArtifactIndex index)
        {
            return getArtifactSelectionWeight(index) > 0f;
        }

        static IEnumerable<ArtifactIndex> getAllAvailableArtifactIndices()
        {
            return Enumerable.Range(0, ArtifactCatalog.artifactCount).Select(i => (ArtifactIndex)i).Where(i => canEnableArtifact(i) && !RunArtifactManager.instance.IsArtifactEnabled(i));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RunArtifactManager.instance && getAllAvailableArtifactIndices().Any();
        }

        ArtifactDef _enabledArtifact;

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        public override void OnStart()
        {
            WeightedSelection<ArtifactIndex> artifactIndexSelection = new WeightedSelection<ArtifactIndex>(ArtifactCatalog.artifactCount);

            foreach (ArtifactIndex index in getAllAvailableArtifactIndices())
            {
                artifactIndexSelection.AddChoice(index, getArtifactSelectionWeight(index));
            }

            _enabledArtifact = ArtifactCatalog.GetArtifactDef(artifactIndexSelection.Evaluate(RNG.nextNormalizedFloat));
            RunArtifactManager.instance.SetArtifactEnabledServer(_enabledArtifact, true);
        }

        public override void OnEnd()
        {
            if (!RunArtifactManager.instance)
                return;
            
            RunArtifactManager.instance.SetArtifactEnabledServer(_enabledArtifact, false);
        }
    }
}
