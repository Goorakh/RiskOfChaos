using HG;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class MaterialOverride : MonoBehaviour
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

                Material[] originalMaterials = ArrayUtils.Clone(renderer.sharedMaterials);
                _originalMaterials.Add(renderer, originalMaterials);

                Material[] materials = new Material[originalMaterials.Length];
                ArrayUtils.SetAll(materials, OverrideMaterial);
                renderer.sharedMaterials = materials;
            }
        }

        void OnDestroy()
        {
            foreach (KeyValuePair<Renderer, Material[]> kvp in _originalMaterials)
            {
                Renderer renderer = kvp.Key;
                Material[] originalMaterials = kvp.Value;

                if (renderer)
                {
                    renderer.sharedMaterials = originalMaterials;
                }
            }
        }
    }
}
