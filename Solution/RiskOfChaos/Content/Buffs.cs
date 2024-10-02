using R2API;
using RiskOfChaos.Patches;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace RiskOfChaos.Content
{
    public static class Buffs
    {
        public static readonly BuffDef SetTo1Hp;

        static Buffs()
        {
            // SetTo1Hp
            {
                SetTo1Hp = ScriptableObject.CreateInstance<BuffDef>();
                SetTo1Hp.name = "bd" + nameof(SetTo1Hp);
                SetTo1Hp.isHidden = true;
                SetTo1Hp.isDebuff = true;
                SetTo1Hp.canStack = false;

                RecalculateStatsAPI.GetStatCoefficients += (body, args) =>
                {
                    if (body.HasBuff(SetTo1Hp))
                    {
                        args.baseCurseAdd += 1e15f;
                    }
                };

                CharacterBodyRecalculateStatsHook.PostRecalculateStats += (body) =>
                {
                    if (body.HasBuff(SetTo1Hp))
                    {
                        body.isGlass = true;

                        // Make sure barrier still decays, default behaviour makes barrier decay so small it basically never decays
                        body.barrierDecayRate = body.maxBarrier;
                    }
                };
            }
        }

        internal static void AddBuffDefsTo(NamedAssetCollection<BuffDef> buffDefs)
        {
            buffDefs.Add([
                SetTo1Hp
            ]);
        }
    }
}
