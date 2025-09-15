using HG;
using HG.Coroutines;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities.Assets;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.ScreenEffect
{
    public static class ScreenEffectCatalog
    {
        static ScreenEffectDef[] _screenEffectDefs = [];

        static readonly Dictionary<string, ScreenEffectIndex> _screenEffectNameToIndex = [];

        [ContentInitializer(typeof(AssetLoader))]
        static IEnumerator LoadContent(ContentIntializerArgs args)
        {
            ParallelProgressCoroutine parallelCoroutine = new ParallelProgressCoroutine(args.ProgressReceiver);

            AssetLoadOperation<Material> screenMaterialLoad = AssetLoader.LoadAssetAsync<Material>("RepeatScreen");
            if (screenMaterialLoad != null)
            {
                screenMaterialLoad.OnComplete += screenMaterial =>
                {
                    if (!screenMaterial)
                    {
                        Log.Error("Failed to load RepeatScreen material");
                        return;
                    }

                    args.ContentPack.screenEffectDefs.Add([
                        new ScreenEffectDef
                        {
                            EffectIndex = ScreenEffectIndex.Invalid,
                            Name = "RepeatScreen",
                            EffectMaterial = screenMaterial,
                            EffectType = ScreenEffectType.UIAndWorld
                        }
                    ]);
                };

                parallelCoroutine.Add(screenMaterialLoad);
            }
            else
            {
                Log.Error("Failed to start load operation for RepeatScreen material");
            }

            return parallelCoroutine;
        }

        [SystemInitializer]
        static void Init()
        {
            ArrayUtils.CloneTo(Main.Instance.ContentPackProvider.ScreenEffectDefs, ref _screenEffectDefs);

            Array.Sort(_screenEffectDefs, (a, b) => string.CompareOrdinal(a.Name, b.Name));

            _screenEffectNameToIndex.Clear();
            _screenEffectNameToIndex.EnsureCapacity(_screenEffectDefs.Length);

            for (int i = 0; i < _screenEffectDefs.Length; i++)
            {
                ScreenEffectDef screenEffectDef = _screenEffectDefs[i];
                ScreenEffectIndex screenEffectIndex = (ScreenEffectIndex)i;

                screenEffectDef.EffectIndex = screenEffectIndex;
                _screenEffectNameToIndex.Add(screenEffectDef.Name, screenEffectIndex);
            }
        }

        public static ScreenEffectDef GetScreenEffectDef(ScreenEffectIndex screenEffectIndex)
        {
            return ArrayUtils.GetSafe(_screenEffectDefs, (int)screenEffectIndex, null);
        }

        public static ScreenEffectIndex FindScreenEffectIndex(string name)
        {
            return _screenEffectNameToIndex.GetValueOrDefault(name, ScreenEffectIndex.Invalid);
        }
    }
}
