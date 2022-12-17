using RoR2;

namespace RiskOfChaos.EffectHandling
{
    public struct ChaosEffectActivationCounter
    {
        public static readonly ChaosEffectActivationCounter EmptyCounter = new ChaosEffectActivationCounter(-1);

        public readonly int EffectIndex;

        public int TotalActivations { get; private set; }

        public int StageActivations { get; private set; }

        public ChaosEffectActivationCounter(int effectIndex)
        {
            EffectIndex = effectIndex;

            TotalActivations = 0;
            StageActivations = 0;

            if (effectIndex != -1)
            {
                Stage.onServerStageComplete += Stage_onServerStageComplete;
                Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            }
        }

        void Stage_onServerStageComplete(Stage stage)
        {
            TotalActivations += StageActivations;
            StageActivations = 0;
        }

        void Run_onRunDestroyGlobal(Run run)
        {
            TotalActivations = 0;
            StageActivations = 0;
        }
    }
}
