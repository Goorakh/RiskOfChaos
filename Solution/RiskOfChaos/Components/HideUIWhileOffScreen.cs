using RiskOfChaos.Utilities;
using System;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class HideUIWhileOffScreen : MonoBehaviour
    {
        public RectTransform[] TransformsToConsider = [];

        void FixedUpdate()
        {
            Rect screenRect = new Rect(0f, 0f, Screen.width, Screen.height);

            foreach (RectTransform rectTransform in TransformsToConsider)
            {
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                Rect rect = new Rect(VectorUtils.Min(corners), Vector2.Scale(rectTransform.lossyScale, rectTransform.rect.size));

                if (rectTransform.gameObject.activeSelf)
                {
                    if (!rect.Overlaps(screenRect))
                    {
                        rectTransform.gameObject.SetActive(false);

#if DEBUG
                        Log.Debug($"Hiding {rectTransform.name}");
#endif
                    }
                }
                else
                {
                    if (rect.Overlaps(screenRect))
                    {
                        rectTransform.gameObject.SetActive(true);

#if DEBUG
                        Log.Debug($"Showing {rectTransform.name}");
#endif
                    }
                }
            }
        }
    }
}
