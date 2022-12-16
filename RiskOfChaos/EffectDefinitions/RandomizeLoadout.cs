using RiskOfChaos.EffectHandling;
using RiskOfChaos.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("RandomizeLoadout", DefaultSelectionWeight = 0.7f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public class RandomizeLoadout : BaseEffect
    {
        public override void OnStart()
        {
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

                    if (changedCurrentBody)
                    {
                        playerMaster.Respawn(playerBody.footPosition, playerBody.GetRotation());
                    }
                }
            }
        }

        bool randomizeLoadoutForBodyIndex(Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            return randomizeLoadoutSkills(bodyLoadoutManager, bodyIndex) | randomizeLoadoutSkin(bodyLoadoutManager, bodyIndex);
        }

        static WeightedSelection<uint> getWeightedIndexSelection(int count, uint currentIndex)
        {
            WeightedSelection<uint> skinIndexSelection = new WeightedSelection<uint>(count);
            for (uint skinIndex = 0; skinIndex < count; skinIndex++)
            {
                skinIndexSelection.AddChoice(skinIndex, skinIndex == currentIndex ? 0.7f : 1f);
            }

            return skinIndexSelection;
        }

        uint evaluateWeightedIndexSelection(int count, uint currentIndex)
        {
            return getWeightedIndexSelection(count, currentIndex).Evaluate(RNG.nextNormalizedFloat);
        }

        bool randomizeLoadoutSkills(Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            bool anyChanges = false;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            Loadout.BodyLoadoutManager.BodyInfo bodyInfo = Loadout.BodyLoadoutManager.allBodyInfos[(int)bodyIndex];
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            for (int skillSlotIndex = 0; skillSlotIndex < bodyInfo.skillSlotCount; skillSlotIndex++)
            {
                int variantsCount = bodyInfo.prefabSkillSlots[skillSlotIndex].skillFamily.variants.Length;
                if (variantsCount > 1) // Only 1: No other options, don't bother trying to randomize it
                {
                    uint currentSkillVariantIndex = bodyLoadoutManager.GetSkillVariant(bodyIndex, skillSlotIndex);

                    uint newSkillVariantIndex = evaluateWeightedIndexSelection(variantsCount, currentSkillVariantIndex);

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
            bool anyChanges = false;

            int bodySkinCount = BodyCatalog.GetBodySkins(bodyIndex).Length;
            if (bodySkinCount > 1) // Only 1: No other options, don't bother trying to randomize it
            {
                uint currentSkinIndex = bodyLoadoutManager.GetSkinIndex(bodyIndex);

                uint newSkinIndex = evaluateWeightedIndexSelection(bodySkinCount, currentSkinIndex);

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
