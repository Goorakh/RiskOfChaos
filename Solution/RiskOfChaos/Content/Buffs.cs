using R2API;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Content
{
    static class Buffs
    {
        [ContentInitializer]
        static void LoadContent(BuffDefAssetCollection buffs)
        {
            // SetTo1Hp
            {
                BuffDef setTo1Hp = ScriptableObject.CreateInstance<BuffDef>();
                setTo1Hp.name = "bd" + nameof(RoCContent.Buffs.SetTo1Hp);
                setTo1Hp.isHidden = true;
                setTo1Hp.isDebuff = true;
                setTo1Hp.canStack = false;

                buffs.Add(setTo1Hp);
            }
        }

        [SystemInitializer]
        static void Init()
        {
            RecalculateStatsAPI.GetStatCoefficients += (body, args) =>
            {
                if (body.HasBuff(RoCContent.Buffs.SetTo1Hp))
                {
                    args.baseCurseAdd += 1e15f;
                }
            };

            CharacterBodyRecalculateStatsHook.PostRecalculateStats += (body) =>
            {
                if (body.HasBuff(RoCContent.Buffs.SetTo1Hp))
                {
                    body.isGlass = true;

                    // Make sure barrier still decays, default behaviour makes barrier decay so small it basically never decays
                    body.barrierDecayRate = body.maxBarrier;
                }
            };
        }
    }
}
