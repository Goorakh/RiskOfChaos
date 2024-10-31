using System;
using UnityEngine;

namespace RiskOfChaos.ScreenEffect
{
    public class ScreenEffectDef
    {
        public static readonly Func<ScreenEffectDef, string> NameProvider = d => d.Name;

        public ScreenEffectIndex EffectIndex;

        public string Name;

        public ScreenEffectType EffectType;

        public Material EffectMaterial;
    }
}
