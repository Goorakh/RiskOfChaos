using R2API;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Content
{
    partial class RoCContent
    {
        partial class Buffs
        {
            [ContentInitializer]
            static void LoadContent(BuffDefAssetCollection buffs)
            {
                // SetTo1Hp
                {
                    BuffDef setTo1Hp = ScriptableObject.CreateInstance<BuffDef>();
                    setTo1Hp.name = "bd" + nameof(SetTo1Hp);
                    setTo1Hp.isHidden = true;
                    setTo1Hp.isDebuff = false;
                    setTo1Hp.canStack = false;

                    buffs.Add(setTo1Hp);
                }
            }

            [SystemInitializer]
            static void Init()
            {
                RecalculateStatsAPI.GetStatCoefficients += getCharacterStatCoefficients;

                CharacterBodyRecalculateStatsHook.PostRecalculateStats += CharacterBodyRecalculateStatsHook_PostRecalculateStats;

                On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            }

            static void getCharacterStatCoefficients(CharacterBody body, RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (body.HasBuff(SetTo1Hp))
                {
                    args.baseCurseAdd += 1e15f;
                }
            }

            static void CharacterBodyRecalculateStatsHook_PostRecalculateStats(CharacterBody body)
            {
                if (body.HasBuff(SetTo1Hp))
                {
                    body.isGlass = true;

                    // Make sure barrier still decays, default behaviour makes barrier decay so small it basically never decays
                    body.barrierDecayRate = body.maxBarrier;
                }
            }

            static void CharacterBody_OnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
            {
                orig(self, buffDef);

                if (buffDef == SetTo1Hp)
                {
                    if (NetworkServer.active)
                    {
                        HealthComponent healthComponent = self.healthComponent;
                        if (healthComponent)
                        {
                            healthComponent.Networkbarrier = 0f;
                        }

                        if (self.isPlayerControlled)
                        {
                            self.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1f);

                            Util.CleanseBody(self, false, false, false, true, false, false);
                        }
                    }
                }
            }
        }
    }
}
