using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Skills;
using RoR2.Stats;
using System;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("RandomizeLoadout", DefaultSelectionWeight = 0.7f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public class RandomizeLoadout : BaseEffect
    {
        public override void OnStart()
        {
            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(false))
            {
                StatSheet playerStatSheet = PlayerStatsComponent.FindMasterStatSheet(playerMaster);

                CharacterBody playerBody = playerMaster.GetBody();

                Loadout loadout = playerMaster.loadout;
                Loadout.BodyLoadoutManager bodyLoadoutManager = loadout.bodyLoadoutManager;

                bool anyChanges = false;
                bool changedCurrentBody = false;

                for (BodyIndex bodyIndex = 0; bodyIndex < (BodyIndex)BodyCatalog.bodyCount; bodyIndex++)
                {
                    bool anyChangesForThisBodyIndex = randomizeLoadoutForBodyIndex(bodyLoadoutManager, playerStatSheet, bodyIndex);
                    anyChanges |= anyChangesForThisBodyIndex;

                    if (anyChangesForThisBodyIndex && bodyIndex == playerBody.bodyIndex)
                    {
                        changedCurrentBody = true;
                    }
                }
                
                if (anyChanges)
                {
                    // Set dirty bit
                    playerMaster.SetLoadoutServer(loadout);

                    if (changedCurrentBody && !playerMaster.IsDeadAndOutOfLivesServer())
                    {
                        respawnPlayerBody(playerMaster, playerBody);
                    }
                }
            }
        }

        static void respawnPlayerBody(CharacterMaster playerMaster, CharacterBody playerBody)
        {
            const string LOG_PREFIX = $"{nameof(RandomizeLoadout)}.{nameof(respawnPlayerBody)} ";

            VehicleSeat oldVehicleSeat = playerBody.currentVehicle;

#if DEBUG
            Log.Debug(LOG_PREFIX + $"seat={oldVehicleSeat}");
#endif

            if (oldVehicleSeat)
            {
                oldVehicleSeat.EjectPassenger();
            }

            PreventMetamorphosisRespawn.PreventionEnabled = true;
            playerBody = playerMaster.Respawn(playerBody.footPosition, playerBody.GetRotation());
            PreventMetamorphosisRespawn.PreventionEnabled = false;

            if (oldVehicleSeat)
            {
                oldVehicleSeat.AssignPassenger(playerBody.gameObject);
            }
        }

        bool randomizeLoadoutForBodyIndex(Loadout.BodyLoadoutManager bodyLoadoutManager, StatSheet playerStatSheet, BodyIndex bodyIndex)
        {
            return randomizeLoadoutSkills(bodyLoadoutManager, playerStatSheet, bodyIndex) | randomizeLoadoutSkin(bodyLoadoutManager, playerStatSheet, bodyIndex);
        }

        static WeightedSelection<uint> getWeightedIndexSelection(int count, uint currentIndex, Predicate<uint> canSelectIndex)
        {
            WeightedSelection<uint> skinIndexSelection = new WeightedSelection<uint>(count);
            for (uint skinIndex = 0; skinIndex < count; skinIndex++)
            {
                if (canSelectIndex == null || canSelectIndex(skinIndex))
                {
                    skinIndexSelection.AddChoice(skinIndex, skinIndex == currentIndex ? 0.7f : 1f);
                }
            }

            return skinIndexSelection;
        }

        uint evaluateWeightedIndexSelection(int count, uint currentIndex, Predicate<uint> canSelectIndex)
        {
            return getWeightedIndexSelection(count, currentIndex, canSelectIndex).Evaluate(RNG.nextNormalizedFloat);
        }

        bool randomizeLoadoutSkills(Loadout.BodyLoadoutManager bodyLoadoutManager, StatSheet playerStatSheet, BodyIndex bodyIndex)
        {
            bool anyChanges = false;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            Loadout.BodyLoadoutManager.BodyInfo bodyInfo = Loadout.BodyLoadoutManager.allBodyInfos[(int)bodyIndex];
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            for (int skillSlotIndex = 0; skillSlotIndex < bodyInfo.skillSlotCount; skillSlotIndex++)
            {
                SkillFamily.Variant[] skillVariants = bodyInfo.prefabSkillSlots[skillSlotIndex].skillFamily.variants;

                int variantsCount = skillVariants.Length;
                if (variantsCount > 1) // Only 1: No other options, don't bother trying to randomize it
                {
                    uint currentSkillVariantIndex = bodyLoadoutManager.GetSkillVariant(bodyIndex, skillSlotIndex);

                    uint newSkillVariantIndex = evaluateWeightedIndexSelection(variantsCount, currentSkillVariantIndex, skillIndex =>
                    {
                        if (!ArrayUtils.IsInBounds(skillVariants, skillIndex))
                            return false;

                        SkillFamily.Variant variant = skillVariants[skillIndex];
                        return !variant.unlockableDef || (playerStatSheet != null && playerStatSheet.HasUnlockable(variant.unlockableDef));
                    });

                    if (currentSkillVariantIndex != newSkillVariantIndex)
                    {
                        anyChanges = true;

                        bodyLoadoutManager.SetSkillVariant(bodyIndex, skillSlotIndex, newSkillVariantIndex);
                    }
                }
            }

            return anyChanges;
        }

        bool randomizeLoadoutSkin(Loadout.BodyLoadoutManager bodyLoadoutManager, StatSheet playerStatSheet, BodyIndex bodyIndex)
        {
            bool anyChanges = false;

            int bodySkinCount = BodyCatalog.GetBodySkins(bodyIndex).Length;
            if (bodySkinCount > 1) // Only 1: No other options, don't bother trying to randomize it
            {
                uint currentSkinIndex = bodyLoadoutManager.GetSkinIndex(bodyIndex);

                uint newSkinIndex = evaluateWeightedIndexSelection(bodySkinCount, currentSkinIndex, skinIndex =>
                {
                    SkinDef skinDef = SkinCatalog.GetBodySkinDef(bodyIndex, (int)skinIndex);
                    return skinDef && (!skinDef.unlockableDef || (playerStatSheet != null && playerStatSheet.HasUnlockable(skinDef.unlockableDef)));
                });

                if (currentSkinIndex != newSkinIndex)
                {
                    anyChanges = true;

                    bodyLoadoutManager.SetSkinIndex(bodyIndex, newSkinIndex);
                }
            }

            return anyChanges;
        }
    }
}
