using R2API;
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

                On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
                {
                    orig(self);

                    if (self.HasBuff(SetTo1Hp))
                    {
                        // Use shatter death animation
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        self.isGlass = true;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                        // Make sure barrier still decays, default behaviour means barrier decay will be so small it basically will never decay
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        self.barrierDecayRate = self.maxBarrier;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    }
                };
            }
        }

        internal static void AddBuffDefsTo(NamedAssetCollection<BuffDef> buffDefs)
        {
            buffDefs.Add(new BuffDef[]
            {
                SetTo1Hp
            });
        }
    }
}
