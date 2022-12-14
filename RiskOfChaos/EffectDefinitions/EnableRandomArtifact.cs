using RiskOfChaos.EffectHandling;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("EnableRandomArtifact")]
    public class EnableRandomArtifact : BaseEffect
    {
        static readonly HashSet<ArtifactIndex> _artifactsToDisableNextStage = new HashSet<ArtifactIndex>();

        static EnableRandomArtifact()
        {
            Stage.onServerStageComplete += Stage_onServerStageComplete;
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

        static IEnumerable<ArtifactIndex> getAllNonEnabledArtifactIndices()
        {
            return Enumerable.Range(0, ArtifactCatalog.artifactCount).Select(i => (ArtifactIndex)i).Where(i => !RunArtifactManager.instance.IsArtifactEnabled(i));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RunArtifactManager.instance && getAllNonEnabledArtifactIndices().Any();
        }

        public override void OnStart()
        {
            ArtifactIndex artifactIndex = RNG.NextElementUniform(getAllNonEnabledArtifactIndices().ToArray());

            RunArtifactManager.instance.SetArtifactEnabledServer(ArtifactCatalog.GetArtifactDef(artifactIndex), true);

            _artifactsToDisableNextStage.Add(artifactIndex);
        }
    }
}
