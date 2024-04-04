using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageEffectApplier : MonoBehaviour
{
    public Material EffectMaterial;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (EffectMaterial)
        {
            Graphics.Blit(source, destination, EffectMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
