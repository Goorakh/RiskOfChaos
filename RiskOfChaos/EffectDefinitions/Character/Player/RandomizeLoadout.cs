using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("RandomizeLoadout", DefaultSelectionWeight = 0.7f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public class RandomizeLoadout : BaseEffect
    {
        public override void OnStart()
        {
            bool isMetamorphosisActive = RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.randomSurvivorOnRespawnArtifactDef);

            CharacterMaster localUserMaster = LocalUserManager.GetFirstLocalUser()?.cachedMaster;

            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(false))
            {
                CharacterBody playerBody = playerMaster.GetBody();

                Loadout loadout = playerMaster.loadout;
                Loadout.BodyLoadoutManager bodyLoadoutManager = loadout.bodyLoadoutManager;

                bool anyChanges = false;
                bool changedCurrentBody = false;

                for (BodyIndex bodyIndex = 0; bodyIndex < (BodyIndex)BodyCatalog.bodyCount; bodyIndex++)
                {
                    bool anyChangesForThisBodyIndex = randomizeLoadoutForBodyIndex(bodyLoadoutManager, bodyIndex);
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
                        respawnPlayerBody(playerMaster, playerBody, isMetamorphosisActive);

                        if (playerMaster == localUserMaster)
                        {
                            // Fix the chat
                            ChatBox chatBox = GameObject.FindObjectOfType<ChatBox>();
                            if (chatBox)
                            {
                                chatBox.ScrollToBottom();
                            }
                            else
                            {
#if DEBUG
                                Log.Debug("no chatbox instance");
#endif
                            }
                        }
                    }
                }
            }
        }

        static void respawnPlayerBody(CharacterMaster playerMaster, CharacterBody playerBody, bool preventMetamorphosisRespawn)
        {
            VehicleSeat oldVehicleSeat = playerBody.currentVehicle;

#if DEBUG
            Log.Debug($"seat={oldVehicleSeat}");
#endif

            if (oldVehicleSeat)
            {
                oldVehicleSeat.EjectPassenger();
            }

            PreventMetamorphosisRespawn.PreventionEnabled = preventMetamorphosisRespawn;
            playerBody = playerMaster.Respawn(playerBody.footPosition, playerBody.GetRotation());
            PreventMetamorphosisRespawn.PreventionEnabled = false;

            if (oldVehicleSeat)
            {
                oldVehicleSeat.AssignPassenger(playerBody.gameObject);
            }
        }

        bool randomizeLoadoutForBodyIndex(Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            return randomizeLoadoutSkills(bodyLoadoutManager, bodyIndex) | randomizeLoadoutSkin(bodyLoadoutManager, bodyIndex);
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

        bool randomizeLoadoutSkills(Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            // TODO: Find a proper solution for client players in multiplayer
            UserProfile userProfile = LocalUserManager.GetFirstLocalUser()?.userProfile;

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
                        return !variant.unlockableDef || (userProfile != null && userProfile.HasUnlockable(variant.unlockableDef));
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

        bool randomizeLoadoutSkin(Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            int bodySkinCount = BodyCatalog.GetBodySkins(bodyIndex).Length;
            if (bodySkinCount > 1) // Only 1: No other options, don't bother trying to randomize it
            {
                uint currentSkinIndex = bodyLoadoutManager.GetSkinIndex(bodyIndex);

                // TODO: Find a proper solution for client players in multiplayer
                UserProfile userProfile = LocalUserManager.GetFirstLocalUser()?.userProfile;
                uint newSkinIndex = evaluateWeightedIndexSelection(bodySkinCount, currentSkinIndex, skinIndex =>
                {
                    SkinDef skinDef = SkinCatalog.GetBodySkinDef(bodyIndex, (int)skinIndex);
                    return skinDef && (!skinDef.unlockableDef || (userProfile != null && userProfile.HasUnlockable(skinDef.unlockableDef)));
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
