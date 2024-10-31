using System;
using UnityEngine;

namespace RiskOfChaos.Components.MaterialInterpolation
{
    public interface IMaterialProvider
    {
        Material Material { get; set; }

        event Action OnPropertiesChanged;
    }
}
