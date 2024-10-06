using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("enable_random_artifact", TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: Enable Random Artifact (Lasts 1 Stage)")]
    public sealed class EnableRandomArtifact : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        readonly struct ArtifactConfig
        {
            public readonly string ArtifactName;
            public readonly ConfigHolder<float> SelectionWeight;

            public ArtifactConfig(ArtifactIndex artifactIndex)
            {
                ArtifactDef artifact = ArtifactCatalog.GetArtifactDef(artifactIndex);
                if (!artifact)
                {
                    ArtifactName = artifactIndex.ToString();
                    Log.Error($"Invalid artifact index {artifactIndex}");
                    return;
                }

                ArtifactName = Language.GetString(artifact.nameToken, "en");

                SelectionWeight =
                    ConfigFactory<float>.CreateConfig($"{ArtifactName} Weight", 1f)
                                        .Description($"""
                                         How likely the {ArtifactName} is to be picked, higher value means more likely, lower value means less likely.

                                         A value of 0 will exclude it completely
                                         """)
                                        .AcceptableValues(new AcceptableValueMin<float>(0f))
                                        .OptionConfig(new FloatFieldConfig { Min = 0f })
                                        .Build();
            }

            public readonly void Bind(ChaosEffectInfo effectInfo)
            {
                SelectionWeight?.Bind(effectInfo);
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

            foreach (ArtifactConfig config in _artifactConfigs.OrderBy(a => a.ArtifactName))
            {
                config.Bind(_effectInfo);
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
            return config?.SelectionWeight?.Value ?? 0f;
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

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            WeightedSelection<ArtifactIndex> artifactIndexSelection = new WeightedSelection<ArtifactIndex>(ArtifactCatalog.artifactCount);

            foreach (ArtifactIndex index in getAllAvailableArtifactIndices())
            {
                artifactIndexSelection.AddChoice(index, getArtifactSelectionWeight(index));
            }

            _enabledArtifact = ArtifactCatalog.GetArtifactDef(artifactIndexSelection.Evaluate(RNG.nextNormalizedFloat));
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.WritePackedIndex32((int)_enabledArtifact.artifactIndex);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _enabledArtifact = ArtifactCatalog.GetArtifactDef((ArtifactIndex)reader.ReadPackedIndex32());
        }

        public override void OnStart()
        {
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
