using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.UI
{
    public static class UISkins
    {
        public static UISkinData ActiveEffectsPanel { get; }

        static UISkins()
        {
            ActiveEffectsPanel = ScriptableObject.CreateInstance<UISkinData>();
            ActiveEffectsPanel.name = "skinActiveEffectsPanel";
        }

        [SystemInitializer]
        static IEnumerator Init()
        {
            List<AsyncOperationHandle> asyncOperations = [];

            AsyncOperationHandle<Sprite> texUICutOffCornerLoad = AddressableUtil.LoadAssetAsync<Sprite>(AddressableGuids.RoR2_Base_UI_texUICutOffCorner_png);
            texUICutOffCornerLoad.OnSuccess(texUICutOffCorner =>
            {
                ActiveEffectsPanel.mainPanelStyle = new UISkinData.PanelStyle
                {
                    material = null,
                    sprite = texUICutOffCorner,
                    color = new Color(0f, 0f, 0f, 0.5451f)
                };

                ActiveEffectsPanel.headerPanelStyle = ActiveEffectsPanel.mainPanelStyle;
                ActiveEffectsPanel.detailPanelStyle = ActiveEffectsPanel.mainPanelStyle;

                ActiveEffectsPanel.bodyTextStyle = new UISkinData.TextStyle
                {
                    font = null,
                    fontSize = 12f,
                    color = Color.white,
                    alignment = TextAlignmentOptions.Center
                };

                ActiveEffectsPanel.headerTextStyle = new UISkinData.TextStyle
                {
                    font = null,
                    fontSize = 14f,
                    color = Color.white,
                    alignment = TextAlignmentOptions.Center
                };

                ActiveEffectsPanel.detailTextStyle = new UISkinData.TextStyle
                {
                    font = null,
                    fontSize = 10f,
                    color = Color.white,
                    alignment = TextAlignmentOptions.Center
                };
            });

            asyncOperations.Add(texUICutOffCornerLoad);

            yield return asyncOperations.WaitForAllLoaded();
        }
    }
}
