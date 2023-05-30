using R2API;
using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using RoR2.UI;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("start_credits", IsNetworked = true)]
    [ChaosTimedEffect(120f, AllowDuplicates = false)]
    public sealed class StartCredits : TimedEffect
    {
        static GameObject _creditsPanelPrefab;

        [SystemInitializer]
        static void Init()
        {
            _creditsPanelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/CreditsPanel.prefab").WaitForCompletion().InstantiateClone("CreditsPanel_NoBackground", false);

            Transform creditsPanelTransform = _creditsPanelPrefab.transform;

            Transform backdrop = creditsPanelTransform.Find("Backdrop");
            if (backdrop)
            {
                backdrop.gameObject.SetActive(false);
            }

            Transform foreground = creditsPanelTransform.Find("Foreground");
            if (foreground)
            {
                foreground.gameObject.SetActive(false);
            }

            Transform viewport = creditsPanelTransform.Find("MainArea/Viewport");
            if (viewport)
            {
                if (viewport.TryGetComponent(out Image backgroundImage))
                {
                    GameObject.Destroy(backgroundImage);
                }

                Transform creditsContent = viewport.Find("CreditsContent");
                if (creditsContent)
                {
                    if (creditsContent.TryGetComponent(out Image moreBackgroundImage))
                    {
                        GameObject.Destroy(moreBackgroundImage);
                    }

                    Transform backgroundStamps = creditsContent.Find("BackgroundStamps");
                    if (backgroundStamps)
                    {
                        HideUIWhileOffScreen hideUIWhileOffScreen = backgroundStamps.gameObject.AddComponent<HideUIWhileOffScreen>();
                        hideUIWhileOffScreen.TransformsToConsider = Array.ConvertAll(backgroundStamps.GetComponentsInChildren<Image>(), i => i.rectTransform);
                    }
                }
            }

            Transform fadePanel = creditsPanelTransform.Find("FadePanel");
            if (fadePanel)
            {
                fadePanel.gameObject.SetActive(false);
            }

            Transform musicOverride = creditsPanelTransform.Find("MusicOverride");
            if (musicOverride)
            {
                GameObject.Destroy(musicOverride.gameObject);
            }

            CreditsPanelController creditsPanelController = _creditsPanelPrefab.GetComponent<CreditsPanelController>();
            creditsPanelController.introDuration = 0f;
            creditsPanelController.scrollDuration = 118f;
            creditsPanelController.outroDuration = 5f;
        }

        GameObject _creditsPanel;

        public override void OnStart()
        {
            RoR2Application.onFixedUpdate += fixedUpdate;
        }

        void fixedUpdate()
        {
            if (!_creditsPanel)
            {
                _creditsPanel = GameObject.Instantiate(_creditsPanelPrefab, RoR2Application.instance.mainCanvas.transform);

                CreditsPanelController creditsPanelController = _creditsPanel.GetComponent<CreditsPanelController>();
                EntityStateMachine stateMachine = _creditsPanel.GetComponent<EntityStateMachine>();
                if (creditsPanelController && stateMachine)
                {
                    float totalCreditsDuration = creditsPanelController.introDuration + creditsPanelController.scrollDuration + creditsPanelController.outroDuration;
                    float age = TimeElapsed % totalCreditsDuration;

                    bool trySkipToState<T>(float duration) where T : CreditsPanelController.BaseCreditsPanelState, new()
                    {
                        if (age > duration)
                        {
                            if (stateMachine.state is not T state)
                            {
                                state = new T();
                                stateMachine.SetState(state);
                            }

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                            state.age = age - duration;
                            state.fixedAge = age - duration;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                            return true;
                        }

                        return false;
                    }

                    if (age > 0)
                    {
                        if (!trySkipToState<CreditsPanelController.OutroState>(creditsPanelController.introDuration + creditsPanelController.scrollDuration))
                        {
                            if (!trySkipToState<CreditsPanelController.MainScrollState>(creditsPanelController.introDuration))
                            {
                                if (!trySkipToState<CreditsPanelController.IntroState>(0f))
                                {
                                    Log.Warning($"Credits state {stateMachine.state} at age {age} not accounted for");
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= fixedUpdate;

            GameObject.Destroy(_creditsPanel);
        }
    }
}
