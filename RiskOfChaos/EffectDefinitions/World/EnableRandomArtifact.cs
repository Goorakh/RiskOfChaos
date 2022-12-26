using BepInEx.Configuration;
using HG;
using RiskOfChaos.EffectHandling;
using RiskOfOptions.Options;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using RiskOfOptions.OptionConfigs;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect(EFFECT_ID)]
    public class EnableRandomArtifact : BaseEffect
    {
        const string EFFECT_ID = "EnableRandomArtifact";

        static string _configSectionName;

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

                SelectionWeight = Main.Instance.Config.Bind(new ConfigDefinition(_configSectionName, $"{artifactName} Weight"), 1f, new ConfigDescription($"How likely the {artifactName} is to be picked, higher value means more likely, lower value means less likely.\n\nA value of 0 will exclude it completely"));
                ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(SelectionWeight, new StepSliderConfig
                {
                    formatString = "{0:F1}",
                    increment = 0.1f,
                    min = 0f,
                    max = 2.5f
                }));
            }
        }
        static ArtifactConfig[] _artifactConfigs;

        static readonly HashSet<ArtifactIndex> _artifactsToDisableNextStage = new HashSet<ArtifactIndex>();

        static EnableRandomArtifact()
        {
            Stage.onServerStageComplete += Stage_onServerStageComplete;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        [SystemInitializer(typeof(ArtifactCatalog), typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _configSectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            _artifactConfigs = new ArtifactConfig[ArtifactCatalog.artifactCount];
            for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                _artifactConfigs[i] = new ArtifactConfig((ArtifactIndex)i);
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

        static void Run_onRunDestroyGlobal(Run run)
        {
            _artifactsToDisableNextStage.Clear();
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
