using RoR2;
using RoR2.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.UI
{
    public static class UISkins
    {
        public static UISkinData ActiveEffectsPanel { get; private set; }

        [SystemInitializer]
        static IEnumerator Init()
        {
            AsyncOperationHandle<Sprite> texUICutOffCornerLoad = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUICutOffCorner.png");

            while (!texUICutOffCornerLoad.IsDone)
            {
                yield return null;
            }

            Sprite texUICutOffCorner = texUICutOffCornerLoad.Result;

            ActiveEffectsPanel = ScriptableObject.CreateInstance<UISkinData>();
            ActiveEffectsPanel.name = "skinActiveEffectsPanel";

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

            yield break;
        }
    }
}
