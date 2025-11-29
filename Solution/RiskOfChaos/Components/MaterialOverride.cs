using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public sealed class MaterialOverride : MonoBehaviour
    {
        readonly Dictionary<Renderer, Material[]> _originalMaterials = [];

        public bool IgnoreParticleRenderers;
        public bool IgnoreDecals;

        public Material OverrideMaterial;

        void Start()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            _originalMaterials.EnsureCapacity(renderers.Length);

            foreach (Renderer renderer in renderers)
            {
                if (IgnoreParticleRenderers && renderer is ParticleSystemRenderer)
                    continue;

                if (IgnoreDecals && renderer.GetComponent("Decal"))
                    continue;

                Material[] originalMaterials = [.. renderer.sharedMaterials];
                _originalMaterials.Add(renderer, originalMaterials);

                Material[] materials = new Material[originalMaterials.Length];
                Array.Fill(materials, OverrideMaterial);
                renderer.sharedMaterials = materials;
            }
        }

        void OnDestroy()
        {
            foreach ((Renderer renderer, Material[] originalMaterials) in _originalMaterials)
            {
                if (renderer)
                {
                    renderer.sharedMaterials = originalMaterials;
                }
            }
        }
    }
}
