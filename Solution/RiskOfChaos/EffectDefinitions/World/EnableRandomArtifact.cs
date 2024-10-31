using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
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
    public sealed class EnableRandomArtifact : NetworkBehaviour
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

        ChaosEffectComponent _effectComponent;

        [SerializedMember("a")]
        ArtifactIndex _enabledArtifact = ArtifactIndex.None;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            WeightedSelection<ArtifactIndex> artifactIndexSelection = new WeightedSelection<ArtifactIndex>();
            artifactIndexSelection.EnsureCapacity(ArtifactCatalog.artifactCount);

            foreach (ArtifactIndex index in getAllAvailableArtifactIndices())
            {
                artifactIndexSelection.AddChoice(index, getArtifactSelectionWeight(index));
            }

            if (artifactIndexSelection.Count > 0)
            {
                _enabledArtifact = artifactIndexSelection.Evaluate(rng.nextNormalizedFloat);
            }
        }

        void Start()
        {
            if (NetworkServer.active && _enabledArtifact != ArtifactIndex.None)
            {
                RunArtifactManager.instance.SetArtifactEnabledServer(ArtifactCatalog.GetArtifactDef(_enabledArtifact), true);
            }
        }

        void OnDestroy()
        {
            if (!NetworkServer.active || !RunArtifactManager.instance)
                return;

            RunArtifactManager.instance.SetArtifactEnabledServer(ArtifactCatalog.GetArtifactDef(_enabledArtifact), false);
        }
    }
}
