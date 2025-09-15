using R2API;
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
            static void LoadContent(ContentIntializerArgs args)
            {
                BuffDef setTo1Hp;
                {
                    setTo1Hp = ScriptableObject.CreateInstance<BuffDef>();
                    setTo1Hp.name = "bd" + nameof(SetTo1Hp);
                    setTo1Hp.isHidden = true;
                    setTo1Hp.isDebuff = false;
                    setTo1Hp.canStack = false;
                }

                args.ContentPack.buffDefs.Add([setTo1Hp]);
            }

            [SystemInitializer]
            static void Init()
            {
                RecalculateStatsAPI.GetStatCoefficients += getCharacterStatCoefficients;

                CharacterBodyEvents.PostRecalculateStats += CharacterBody_PostRecalculateStats;
                CharacterBodyEvents.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            }

            static void getCharacterStatCoefficients(CharacterBody body, RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (body.HasBuff(SetTo1Hp) && !body.isBoss && !body.isChampion)
                {
                    args.baseCurseAdd += 1e15f;
                }
            }

            static void CharacterBody_PostRecalculateStats(CharacterBody body)
            {
                if (body.HasBuff(SetTo1Hp) && !body.isBoss && !body.isChampion)
                {
                    body.isGlass = true;

                    // Make sure barrier still decays, default behaviour makes barrier decay so small it basically never decays
                    body.barrierDecayRate = Mathf.Max(body.barrierDecayRate, body.maxBarrier);
                }
            }

            static void CharacterBody_OnBuffFirstStackGained(CharacterBody body, BuffDef buffDef)
            {
                if (buffDef == SetTo1Hp)
                {
                    if (NetworkServer.active)
                    {
                        HealthComponent healthComponent = body.healthComponent;
                        if (healthComponent)
                        {
                            healthComponent.Networkbarrier = 0f;
                        }

                        if (body.isPlayerControlled)
                        {
                            body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1f);

                            Util.CleanseBody(body, false, false, false, true, false, false);
                        }
                    }
                }
            }
        }
    }
}
