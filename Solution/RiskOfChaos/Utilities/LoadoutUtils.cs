﻿using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class LoadoutUtils
    {
        [Flags]
        public enum GeneratorFlags : byte
        {
            None = 0,
            Skills = 1 << 0,
            Skin = 1 << 1,
            All = byte.MaxValue
        }

        public static Loadout GetRandomLoadoutFor(BodyIndex bodyIndex, Xoroshiro128Plus rng, GeneratorFlags flags = GeneratorFlags.All)
        {
            Loadout.BodyLoadoutManager.BodyInfo bodyInfo = Loadout.BodyLoadoutManager.allBodyInfos[(int)bodyIndex];

            Loadout loadout = new Loadout();

            if ((flags & GeneratorFlags.Skills) != 0)
            {
                for (int i = 0; i < bodyInfo.skillSlotCount; i++)
                {
                    SkillFamily.Variant[] skillVariants = bodyInfo.prefabSkillSlots[i].skillFamily.variants;

                    if (skillVariants.Length > 0)
                    {
                        try
                        {
                            loadout.bodyLoadoutManager.SetSkillVariant(bodyIndex, i, (uint)rng.RangeInt(0, skillVariants.Length));
                        }
                        catch (Exception e)
                        {
                            Log.Error_NoCallerPrefix($"Failed to set {BodyCatalog.GetBodyName(bodyIndex)} {(SkillSlot)i} skill: {e}");
                        }
                    }
                }
            }

            if ((flags & GeneratorFlags.Skin) != 0)
            {
                int bodySkinCount = SkinCatalog.GetBodySkinCount(bodyIndex);
                if (bodySkinCount > 0)
                {
                    try
                    {
                        loadout.bodyLoadoutManager.SetSkinIndex(bodyIndex, (uint)rng.RangeInt(0, bodySkinCount));
                    }
                    catch (Exception e)
                    {
                        Log.Error_NoCallerPrefix($"Failed to set {BodyCatalog.GetBodyName(bodyIndex)} skin: {e}");
                    }
                }
            }

            return loadout;
        }

        public static Loadout GetRandomLoadoutFor(CharacterMaster master, Xoroshiro128Plus rng, GeneratorFlags flags = GeneratorFlags.All)
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
                    return GetRandomLoadoutFor(bodyIndex, rng, flags);
                }
            }

            return null;
        }

        public static Loadout GetRandomLoadoutFor(GameObject characterPrefab, Xoroshiro128Plus rng, GeneratorFlags flags = GeneratorFlags.All)
        {
            if (!characterPrefab)
                return null;

            if (characterPrefab.TryGetComponent(out CharacterBody characterBody))
            {
                return GetRandomLoadoutFor(characterBody.bodyIndex, rng, flags);
            }
            else if (characterPrefab.TryGetComponent(out CharacterMaster characterMaster))
            {
                return GetRandomLoadoutFor(characterMaster, rng, flags);
            }
            else
            {
                Log.Warning($"{characterPrefab} has no character related components");
                return null;
            }
        }

        public static Loadout GetRandomLoadoutFor(CharacterSpawnCard spawnCard, Xoroshiro128Plus rng, GeneratorFlags flags = GeneratorFlags.All)
        {
            if (!spawnCard)
                return null;

            return GetRandomLoadoutFor(spawnCard.prefab, rng, flags);
        }
    }
}
