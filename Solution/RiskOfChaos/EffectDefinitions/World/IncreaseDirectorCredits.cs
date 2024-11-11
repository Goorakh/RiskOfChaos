using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Director;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("increase_director_credits", TimedEffectType.UntilStageEnd, ConfigName = "Increase Monster Spawns")]
    [EffectConfigBackwardsCompatibility("Effect: +50% Director Credits", "Effect: Increase Director Credits")]
    public sealed class IncreaseDirectorCredits : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _creditIncrease =
            ConfigFactory<float>.CreateConfig("Monster Spawn Increase", 0.5f)
                                .RenamedFrom("Credit Increase Amount")
                                .Description("How much to increase monster spawns by")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "+{0:P0}",
                                    min = 0f,
                                    max = 2f,
                                    increment = 0.05f
                                })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return RoCContent.NetworkedPrefabs.DirectorModificationProvider && (!context.IsNow || CombatDirector.instancesList.Count > 0);
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_creditIncrease) { ValueFormat = "P0" };
        }

        ValueModificationController _directorModificationController;

        void Start()
        {
            if (!NetworkServer.active)
                return;

            _directorModificationController = Instantiate(RoCContent.NetworkedPrefabs.DirectorModificationProvider).GetComponent<ValueModificationController>();

            DirectorModificationProvider directorModificationProvider = _directorModificationController.GetComponent<DirectorModificationProvider>();
            directorModificationProvider.CombatDirectorCreditMultiplierConfigBinding.BindToConfig(_creditIncrease, v => 1f + v);

            NetworkServer.Spawn(_directorModificationController.gameObject);
        }

        void OnDestroy()
        {
            if (_directorModificationController)
            {
                _directorModificationController.Retire();
                _directorModificationController = null;
            }
        }
    }
}
