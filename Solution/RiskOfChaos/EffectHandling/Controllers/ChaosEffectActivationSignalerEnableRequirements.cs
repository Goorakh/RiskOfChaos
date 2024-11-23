using RiskOfChaos.ConfigHandling;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1)]
    public class ChaosEffectActivationSignalerEnableRequirements : MonoBehaviour
    {
        ChaosEffectActivationSignaler[] _effectActivationSignalers;

        [SerializeField]
        bool _useRequiredVotingMode;

        [SerializeField]
        Configs.ChatVoting.ChatVotingMode _requiredVotingMode;

        public Configs.ChatVoting.ChatVotingMode? RequiredVotingMode
        {
            get
            {
                return _useRequiredVotingMode ? _requiredVotingMode : null;
            }
            set
            {
                if (value == RequiredVotingMode)
                    return;

                _useRequiredVotingMode = value.HasValue;
                _requiredVotingMode = value.GetValueOrDefault();

                if (!Util.IsPrefab(gameObject))
                {
                    RefreshSignalersEnabled();
                }
            }
        }

        void Awake()
        {
            _effectActivationSignalers = GetComponents<ChaosEffectActivationSignaler>();

            if (!NetworkServer.active)
            {
                Log.Warning("Actived on client");

                enabled = false;
                return;
            }
        }

        void OnEnable()
        {
            RefreshSignalersEnabled();

            Configs.ChatVoting.VotingMode.SettingChanged += onChatVotingModeChanged;
        }

        void OnDisable()
        {
            Configs.ChatVoting.VotingMode.SettingChanged -= onChatVotingModeChanged;

            updateSignalersEnabled(false);
        }

        void onChatVotingModeChanged(object sender, ConfigChangedArgs<Configs.ChatVoting.ChatVotingMode> e)
        {
            if (_useRequiredVotingMode)
            {
                RefreshSignalersEnabled();
            }
        }

        public void RefreshSignalersEnabled()
        {
            bool shouldSignalersBeActive =
                (!_useRequiredVotingMode || Configs.ChatVoting.VotingMode.Value == _requiredVotingMode);

            updateSignalersEnabled(shouldSignalersBeActive);
        }

        void updateSignalersEnabled(bool enabled)
        {
            bool changedEnabledState = false;

            foreach (ChaosEffectActivationSignaler effectSignaler in _effectActivationSignalers)
            {
                if (effectSignaler)
                {
                    if (effectSignaler.enabled != enabled)
                    {
                        changedEnabledState = true;
                        effectSignaler.enabled = enabled;
                    }
                }
            }

            if (changedEnabledState)
            {
                Log.Debug($"{(enabled ? "enabled" : "disabled")} effect activation signaler(s): {name}");
            }
        }
    }
}
