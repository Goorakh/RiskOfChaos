using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class LoadoutUtils
    {
        public static Loadout GetRandomLoadoutFor(BodyIndex bodyIndex, Xoroshiro128Plus rng)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            Loadout.BodyLoadoutManager.BodyInfo bodyInfo = Loadout.BodyLoadoutManager.allBodyInfos[(int)bodyIndex];
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            Loadout loadout = new Loadout();
            for (int i = 0; i < bodyInfo.skillSlotCount; i++)
            {
                SkillFamily.Variant[] skillVariants = bodyInfo.prefabSkillSlots[i].skillFamily.variants;

                if (skillVariants.Length > 0)
                {
                    loadout.bodyLoadoutManager.SetSkillVariant(bodyIndex, i, (uint)rng.RangeInt(0, skillVariants.Length));
                }
            }

            int bodySkinCount = BodyCatalog.GetBodySkins(bodyIndex).Length;
            if (bodySkinCount > 0)
            {
                loadout.bodyLoadoutManager.SetSkinIndex(bodyIndex, (uint)rng.RangeInt(0, bodySkinCount));
            }

            return loadout;
        }

        public static Loadout GetRandomLoadoutFor(CharacterMaster master, Xoroshiro128Plus rng)
        {
            if (master)
            {
                GameObject bodyObject = master.GetBodyObject();
                if (!bodyObject)
                {
                    bodyObject = master.bodyPrefab;
                }

                BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(bodyObject);
                if (bodyIndex != BodyIndex.None)
                {
                    return LoadoutUtils.GetRandomLoadoutFor(bodyIndex, new Xoroshiro128Plus(rng.nextUlong));
                }
            }

            return null;
        }
    }
}
