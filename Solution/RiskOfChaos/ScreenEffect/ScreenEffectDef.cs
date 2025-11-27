using System;
using UnityEngine;

namespace RiskOfChaos.ScreenEffect
{
    public sealed class ScreenEffectDef
    {
        public static readonly Func<ScreenEffectDef, string> NameProvider = d => d.Name;

        public ScreenEffectIndex EffectIndex;

        public string Name;

        public ScreenEffectType EffectType;

        public Material EffectMaterial;
    }
}
