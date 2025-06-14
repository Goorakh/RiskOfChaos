using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace RiskOfChaos.EffectDefinitions.UI
{
    [ChaosTimedEffect("start_credits", 120f, AllowDuplicates = false)]
    public sealed class StartCredits : MonoBehaviour
    {
        [ContentInitializer]
        static IEnumerator LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = [];

            AsyncOperationHandle<GameObject> creditsPanelLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_UI_CreditsPanel_prefab, AsyncReferenceHandleUnloadType.Preload);
            creditsPanelLoad.OnSuccess(creditsPanelPrefab =>
            {
                GameObject prefab = creditsPanelPrefab.InstantiatePrefab(nameof(RoCContent.LocalPrefabs.CreditsPanelNoBackground));

                Transform transform = prefab.transform;

                Transform backdrop = transform.Find("Backdrop");
                if (backdrop)
                {
                    backdrop.gameObject.SetActive(false);
                }

                Transform foreground = transform.Find("Foreground");
                if (foreground)
                {
                    foreground.gameObject.SetActive(false);
                }

                Transform viewport = transform.Find("MainArea/Viewport");
                if (viewport)
                {
                    if (viewport.TryGetComponent(out Image backgroundImage))
                    {
                        Destroy(backgroundImage);
                    }

                    Transform creditsContent = viewport.Find("CreditsContent");
                    if (creditsContent)
                    {
                        if (creditsContent.TryGetComponent(out Image moreBackgroundImage))
                        {
                            Destroy(moreBackgroundImage);
                        }
                    }
                }

                Transform fadePanel = transform.Find("FadePanel");
                if (fadePanel)
                {
                    Destroy(fadePanel.gameObject);
                }

                Transform musicOverride = transform.Find("MusicOverride");
                if (musicOverride)
                {
                    Destroy(musicOverride.gameObject);
                }

                CreditsPanelController creditsPanelController = prefab.GetComponent<CreditsPanelController>();
                creditsPanelController.introDuration = 0f;
                creditsPanelController.scrollDuration = 118f;
                creditsPanelController.outroDuration = 5f;

                localPrefabs.Add(prefab);
            });

            asyncOperations.Add(creditsPanelLoad);

            yield return asyncOperations.WaitForAllLoaded();
        }

        GameObject _creditsPanelObject;

        void FixedUpdate()
        {
            if (!NetworkClient.active)
                return;

            if (!_creditsPanelObject)
            {
                _creditsPanelObject = Instantiate(RoCContent.LocalPrefabs.CreditsPanelNoBackground, RoR2Application.instance.mainCanvas.transform);
            }
        }

        void OnDestroy()
        {
            Destroy(_creditsPanelObject);
        }
    }
}
