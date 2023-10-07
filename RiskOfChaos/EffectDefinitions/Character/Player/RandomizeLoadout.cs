using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Skills;
using System;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("randomize_loadout", DefaultSelectionWeight = 0.7f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class RandomizeLoadout : BaseEffect
    {
        public override void OnStart()
        {
            bool isMetamorphosisActive = RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef);

            PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
            {
                CharacterBody playerBody = playerMaster.GetBody();

                Loadout loadout = playerMaster.loadout;
                Loadout.BodyLoadoutManager bodyLoadoutManager = loadout.bodyLoadoutManager;

                bool anyChanges = false;
                bool changedCurrentBody = false;

                for (BodyIndex bodyIndex = 0; bodyIndex < (BodyIndex)BodyCatalog.bodyCount; bodyIndex++)
                {
                    if (randomizeLoadoutForBodyIndex(playerMaster, bodyLoadoutManager, bodyIndex))
                    {
                        anyChanges = true;

                        if (playerBody && bodyIndex == playerBody.bodyIndex)
                        {
                            changedCurrentBody = true;
                        }
                    }
                }

                if (anyChanges)
                {
                    // Set dirty bit
                    playerMaster.SetLoadoutServer(loadout);

                    if (changedCurrentBody && !playerMaster.IsDeadAndOutOfLivesServer())
                    {
                        PreventMetamorphosisRespawn.PreventionEnabled = isMetamorphosisActive;
                        playerMaster.Respawn(CharacterRespawnFlags.All);
                        PreventMetamorphosisRespawn.PreventionEnabled = false;
                    }
                }
            }, Util.GetBestMasterName);
        }

        bool randomizeLoadoutForBodyIndex(CharacterMaster master, Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            bool skillsChanged;
            try
            {
                skillsChanged = randomizeLoadoutSkills(master, bodyLoadoutManager, bodyIndex);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to randomize {Util.GetBestMasterName(master)} ({BodyCatalog.GetBodyName(bodyIndex)}) skills: {ex}");
                skillsChanged = false;
            }

            bool skinChanged;
            try
            {
                skinChanged = randomizeLoadoutSkin(master, bodyLoadoutManager, bodyIndex);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to randomize {Util.GetBestMasterName(master)} ({BodyCatalog.GetBodyName(bodyIndex)}) skin: {ex}");
                skinChanged = false;
            }

            return skillsChanged || skinChanged;
        }

        static WeightedSelection<uint> getWeightedIndexSelection(int count, uint currentIndex, Predicate<uint> canSelectIndex)
        {
            WeightedSelection<uint> indexSelection = new WeightedSelection<uint>(count);
            for (uint index = 0; index < count; index++)
            {
                if (canSelectIndex == null || canSelectIndex(index))
                {
                    indexSelection.AddChoice(index, index == currentIndex ? 0.7f : 1f);
                }
            }

            return indexSelection;
        }

        uint evaluateWeightedIndexSelection(int count, uint currentIndex, Predicate<uint> canSelectIndex)
        {
            return getWeightedIndexSelection(count, currentIndex, canSelectIndex).Evaluate(RNG.nextNormalizedFloat);
        }

        bool randomizeLoadoutSkills(CharacterMaster master, Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            NetworkUser networkUser = master && master.playerCharacterMasterController ? master.playerCharacterMasterController.networkUser : null;

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
                        return !variant.unlockableDef || !networkUser || networkUser.unlockables.Contains(variant.unlockableDef);
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

        bool randomizeLoadoutSkin(CharacterMaster master, Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            NetworkUser networkUser = master && master.playerCharacterMasterController ? master.playerCharacterMasterController.networkUser : null;

            int bodySkinCount = BodyCatalog.GetBodySkins(bodyIndex).Length;
            if (bodySkinCount > 1) // Only 1: No other options, don't bother trying to randomize it
            {
                uint currentSkinIndex = bodyLoadoutManager.GetSkinIndex(bodyIndex);

                uint newSkinIndex = evaluateWeightedIndexSelection(bodySkinCount, currentSkinIndex, skinIndex =>
                {
                    SkinDef skinDef = SkinCatalog.GetBodySkinDef(bodyIndex, (int)skinIndex);
                    return skinDef && (!skinDef.unlockableDef || !networkUser || networkUser.unlockables.Contains(skinDef.unlockableDef));
                });

                if (currentSkinIndex != newSkinIndex)
                {
                    bodyLoadoutManager.SetSkinIndex(bodyIndex, newSkinIndex);
                    return true;
                }
            }

            return false;
        }
    }
}
