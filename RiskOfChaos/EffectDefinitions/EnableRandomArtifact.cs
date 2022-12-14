using BepInEx.Configuration;
using HG;
using RiskOfChaos.EffectHandling;
using RiskOfOptions.Options;
using RiskOfOptions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using RiskOfOptions.OptionConfigs;
using System;

namespace RiskOfChaos.EffectDefinitions
{
    // Show the artifact popup on start

    // Kin: UI doesn't update to show the enemy type
    // Sacrifice: On activate: remove all chests?
    // Metamorphosis: Respawn all players?

    [ChaosEffect(EFFECT_ID)]
    public class EnableRandomArtifact : BaseEffect
    {
        const string EFFECT_ID = "EnableRandomArtifact";

        readonly struct ArtifactConfig
        {
            public readonly ConfigEntry<bool> CanEnable;
            public readonly ConfigEntry<float> SelectionWeight;

            public ArtifactConfig(string configSectionName, ArtifactIndex artifactIndex)
            {
                const string LOG_PREFIX = $"{nameof(EnableRandomArtifact)}+{nameof(ArtifactConfig)}..ctor ";

                ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(artifactIndex);
                if (!artifactDef)
                {
                    Log.Error(LOG_PREFIX + $"Invalid artifact index {artifactIndex}");
                    return;
                }

                string artifactName = Language.GetString(artifactDef.nameToken, "en");

                CanEnable = Main.Instance.Config.Bind(new ConfigDefinition(configSectionName, $"{artifactName} Enabled"), true, new ConfigDescription($"If the {artifactName} should be able to be picked"));
                ModSettingsManager.AddOption(new CheckBoxOption(CanEnable), ChaosEffectCatalog.CONFIG_MOD_GUID, ChaosEffectCatalog.CONFIG_MOD_NAME);

                SelectionWeight = Main.Instance.Config.Bind(new ConfigDefinition(configSectionName, $"{artifactName} Weight"), 1f, new ConfigDescription($"How likely the {artifactName} is to be picked, higher value means more likely, lower value means less likely"));
                ModSettingsManager.AddOption(new StepSliderOption(SelectionWeight, new StepSliderConfig
                {
                    formatString = "{0:F1}",
                    increment = 0.1f,
                    min = 0f,
                    max = 2.5f
                }), ChaosEffectCatalog.CONFIG_MOD_GUID, ChaosEffectCatalog.CONFIG_MOD_NAME);
            }
        }
        static ArtifactConfig[] _artifactConfigs;

        static readonly HashSet<ArtifactIndex> _artifactsToDisableNextStage = new HashSet<ArtifactIndex>();

        static EnableRandomArtifact()
        {
            Stage.onServerStageComplete += Stage_onServerStageComplete;
        }

        [SystemInitializer(typeof(ArtifactCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            const string LOG_PREFIX = $"{nameof(EnableRandomArtifact)}.{nameof(Init)} ";

            int index = ChaosEffectCatalog.FindEffectIndex(EFFECT_ID);

            if (index < 0)
            {
                Log.Warning(LOG_PREFIX + $"unable to find effect '{EFFECT_ID}'");
                return;
            }

            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo((uint)index);

            _artifactConfigs = new ArtifactConfig[ArtifactCatalog.artifactCount];
            for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                _artifactConfigs[i] = new ArtifactConfig(effectInfo.ConfigSectionName, (ArtifactIndex)i);
            }
        }

        static void Stage_onServerStageComplete(Stage stage)
        {
            if (_artifactsToDisableNextStage.Count > 0)
            {
                foreach (ArtifactIndex artifactIndex in _artifactsToDisableNextStage)
                {
                    RunArtifactManager.instance.SetArtifactEnabledServer(ArtifactCatalog.GetArtifactDef(artifactIndex), false);
                }

                _artifactsToDisableNextStage.Clear();
            }
        }

        static ArtifactConfig? getArtifactConfig(ArtifactIndex index)
        {
            if (!ArrayUtils.IsInBounds(_artifactConfigs, (int)index))
                return null;

            return _artifactConfigs[(int)index];
        }

        static bool canEnableArtifact(ArtifactIndex index)
        {
            ArtifactConfig? config = getArtifactConfig(index);
            return config.HasValue && config.Value.CanEnable.Value;
        }

        static float getArtifactSelectionWeight(ArtifactIndex index)
        {
            ArtifactConfig? config = getArtifactConfig(index);
            if (!config.HasValue)
                return 0f;

            return config.Value.SelectionWeight.Value;
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

        [EffectWeightMultiplierSelector]
        static float WeightMultSelector()
        {
            return RoCMath.CalcReductionWeight(_artifactsToDisableNextStage.Count, 3.5f);
        }

        public override void OnStart()
        {
            WeightedSelection<ArtifactIndex> artifactIndexSelection = new WeightedSelection<ArtifactIndex>(ArtifactCatalog.artifactCount);

            foreach (ArtifactIndex index in getAllAvailableArtifactIndices())
            {
                artifactIndexSelection.AddChoice(index, getArtifactSelectionWeight(index));
            }

            ArtifactIndex artifactIndex = artifactIndexSelection.Evaluate(RNG.nextNormalizedFloat);

            RunArtifactManager.instance.SetArtifactEnabledServer(ArtifactCatalog.GetArtifactDef(artifactIndex), true);

            _artifactsToDisableNextStage.Add(artifactIndex);
        }
    }
}
