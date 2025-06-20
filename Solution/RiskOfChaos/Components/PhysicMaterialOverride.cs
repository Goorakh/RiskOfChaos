﻿using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public class PhysicMaterialOverride : MonoBehaviour
    {
        readonly Dictionary<Collider, PhysicMaterial> _originalMaterials = [];

        readonly record struct OverrideMaterial(PhysicMaterial Material, int Priority) : IComparable<OverrideMaterial>
        {
            public readonly int CompareTo(OverrideMaterial other)
            {
                return other.Priority.CompareTo(Priority);
            }
        }

        readonly List<OverrideMaterial> _overrideMaterials = [];

        bool _hasAppliedOverrideMaterial;
        PhysicMaterial _appliedOverrideMaterial;

        void Awake()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            _originalMaterials.EnsureCapacity(colliders.Length);

            foreach (Collider collider in colliders)
            {
                if (!collider.isTrigger)
                {
                    _originalMaterials[collider] = collider.sharedMaterial;
                }
            }
        }

        void OnDestroy()
        {
            _overrideMaterials.Clear();
            refreshMaterials();
        }

        void refreshMaterials()
        {
            if (_overrideMaterials.Count > 0)
            {
                setOverrideMaterial(_overrideMaterials[0].Material);
            }
            else
            {
                restoreMaterials();
            }
        }

        void setOverrideMaterial(PhysicMaterial overrideMaterial)
        {
            if (_hasAppliedOverrideMaterial && _appliedOverrideMaterial == overrideMaterial)
                return;

            foreach (Collider collider in _originalMaterials.Keys)
            {
                if (collider)
                {
                    collider.sharedMaterial = overrideMaterial;
                }
            }

            _hasAppliedOverrideMaterial = true;
            _appliedOverrideMaterial = overrideMaterial;

            Log.Debug($"{Util.GetGameObjectHierarchyName(gameObject)} set override material: {overrideMaterial}");
        }

        void restoreMaterials()
        {
            if (!_hasAppliedOverrideMaterial)
                return;

            foreach (KeyValuePair<Collider, PhysicMaterial> kvp in _originalMaterials)
            {
                kvp.Deconstruct(out Collider collider, out PhysicMaterial originalMaterial);

                if (collider)
                {
                    collider.sharedMaterial = originalMaterial;
                }
            }

            _appliedOverrideMaterial = null;
            _hasAppliedOverrideMaterial = false;

            Log.Debug($"{Util.GetGameObjectHierarchyName(gameObject)} reset material");
        }

        public void AddOverrideMaterial(PhysicMaterial material, int priority = 0)
        {
            OverrideMaterial overrideMaterial = new OverrideMaterial(material, priority);

            _overrideMaterials.InsertSorted(overrideMaterial);
            refreshMaterials();
        }

        public void RemoveOverrideMaterial(PhysicMaterial material)
        {
            int materialIndex = _overrideMaterials.FindIndex(o => o.Material == material);
            if (materialIndex != -1)
            {
                _overrideMaterials.RemoveAt(materialIndex);
                refreshMaterials();
            }
        }

        public static PhysicMaterialOverride AddOverrideMaterial(GameObject obj, PhysicMaterial overrideMaterial, int priority = 0)
        {
            PhysicMaterialOverride materialOverrideController = obj.EnsureComponent<PhysicMaterialOverride>();
            materialOverrideController.AddOverrideMaterial(overrideMaterial, priority);
            return materialOverrideController;
        }

        public static void RemoveOverrideMaterial(GameObject obj, PhysicMaterial overrideMaterial)
        {
            if (obj.TryGetComponent(out PhysicMaterialOverride materialOverrideController))
            {
                materialOverrideController.RemoveOverrideMaterial(overrideMaterial);
            }
        }
    }
}
