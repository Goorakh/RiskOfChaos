using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("randomize_loadout", DefaultSelectionWeight = 0.7f)]
    public sealed class RandomizeLoadout : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _randomizeSkills =
            ConfigFactory<bool>.CreateConfig("Randomize Skills", true)
                               .Description("If the effect should randomize character skills")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _randomizeSkin =
            ConfigFactory<bool>.CreateConfig("Randomize Skin", true)
                               .Description("If the effect should randomize character skins")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _randomizeSkills.Value || _randomizeSkin.Value;
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(playerMaster =>
            {
                CharacterBody playerBody = playerMaster.GetBody();

                Loadout loadout = playerMaster.loadout;
                Loadout.BodyLoadoutManager bodyLoadoutManager = loadout.bodyLoadoutManager;

                bool anyChanges = false;
                bool changedCurrentBody = false;
                bool changedCurrentBodySkills = false;
                bool changedCurrentBodySkin = false;

                for (BodyIndex bodyIndex = 0; bodyIndex < (BodyIndex)BodyCatalog.bodyCount; bodyIndex++)
                {
                    if (randomizeLoadoutForBodyIndex(playerMaster, bodyLoadoutManager, bodyIndex, out bool changedSkill, out bool changedSkin))
                    {
                        anyChanges = true;

                        if (playerBody && bodyIndex == playerBody.bodyIndex)
                        {
                            changedCurrentBody = true;

                            changedCurrentBodySkills = changedSkill;
                            changedCurrentBodySkin = changedSkin;
                        }
                    }
                }

                if (anyChanges)
                {
                    // Set dirty bit
                    playerMaster.SetLoadoutServer(loadout);

                    if (changedCurrentBody && playerBody)
                    {
                        playerBody.SetLoadoutServer(loadout);

                        if (changedCurrentBodySkin)
                        {
                            ModelLocator modelLocator = playerBody.modelLocator;
                            if (modelLocator)
                            {
                                Transform modelTransform = modelLocator.modelTransform;
                                if (modelTransform && modelTransform.TryGetComponent(out ModelSkinController modelSkinController))
                                {
                                    modelSkinController.ApplySkin((int)loadout.bodyLoadoutManager.GetSkinIndex(playerBody.bodyIndex));
                                }
                            }
                        }
                    }
                }
            }, Util.GetBestMasterName);
        }

        bool randomizeLoadoutForBodyIndex(CharacterMaster master, Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex, out bool changedSkill, out bool changedSkin)
        {
            changedSkill = tryRandomizeLoadoutSkills(master, bodyLoadoutManager, bodyIndex);
            changedSkin = tryRandomizeLoadoutSkin(master, bodyLoadoutManager, bodyIndex);
            return changedSkill || changedSkin;
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

        bool tryRandomizeLoadoutSkills(CharacterMaster master, Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            if (!_randomizeSkills.Value)
                return false;

            try
            {
                return randomizeLoadoutSkills(master, bodyLoadoutManager, bodyIndex);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to randomize {Util.GetBestMasterName(master)} ({BodyCatalog.GetBodyName(bodyIndex)}) skills: {ex}");
                return false;
            }
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

        bool tryRandomizeLoadoutSkin(CharacterMaster master, Loadout.BodyLoadoutManager bodyLoadoutManager, BodyIndex bodyIndex)
        {
            if (!_randomizeSkin.Value)
                return false;

            try
            {
                return randomizeLoadoutSkin(master, bodyLoadoutManager, bodyIndex);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to randomize {Util.GetBestMasterName(master)} ({BodyCatalog.GetBodyName(bodyIndex)}) skin: {ex}");
                return false;
            }
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
