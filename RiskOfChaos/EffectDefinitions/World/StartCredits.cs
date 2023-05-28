using R2API;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
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
            }
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= fixedUpdate;

            GameObject.Destroy(_creditsPanel);
        }
    }
}
