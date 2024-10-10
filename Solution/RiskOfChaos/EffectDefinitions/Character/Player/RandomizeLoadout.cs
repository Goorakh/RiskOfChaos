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
using System.Collections.Generic;
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
                Xoroshiro128Plus rng = new Xoroshiro128Plus(RNG.nextUlong);

                CharacterBody playerBody = playerMaster.GetBody();

                Loadout loadout = playerMaster.loadout;
                Loadout.BodyLoadoutManager bodyLoadoutManager = loadout.bodyLoadoutManager;

                bool anyChanges = false;
                bool changedCurrentBody = false;
                bool changedCurrentBodySkills = false;
                bool changedCurrentBodySkin = false;

                for (BodyIndex bodyIndex = 0; bodyIndex < (BodyIndex)BodyCatalog.bodyCount; bodyIndex++)
                {
                    if (randomizeLoadoutForBodyIndex(playerMaster, loadout, bodyIndex, rng, out bool changedAnySkill, out bool changedSkin))
                    {
                        anyChanges = true;

                        if (playerBody && bodyIndex == playerBody.bodyIndex)
                        {
                            changedCurrentBody = true;

                            changedCurrentBodySkills = changedAnySkill;
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

        static bool randomizeLoadoutForBodyIndex(CharacterMaster master, Loadout loadout, BodyIndex bodyIndex, Xoroshiro128Plus rng, out bool changedAnySkill, out bool changedSkin)
        {
            NetworkUser networkUser = master && master.playerCharacterMasterController ? master.playerCharacterMasterController.networkUser : null;

            Loadout.BodyLoadoutManager.BodyInfo bodyInfo = Loadout.BodyLoadoutManager.allBodyInfos[(int)bodyIndex];

            uint[] currentSkillVariants = new uint[bodyInfo.skillSlotCount];
            for (int i = 0; i < currentSkillVariants.Length; i++)
            {
                currentSkillVariants[i] = loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, i);
            }

            List<LoadoutSkillPreset> allSkillPresets = generateSkillPresets(networkUser, bodyInfo, currentSkillVariants);

            uint currentSkinIndex = loadout.bodyLoadoutManager.GetSkinIndex(bodyIndex);
            List<LoadoutSkinPreset> allSkinPresets = generateSkinPresets(bodyIndex, networkUser, currentSkinIndex);

            WeightedSelection<LoadoutPreset> loadoutSelection = new WeightedSelection<LoadoutPreset>();
            loadoutSelection.EnsureCapacity(allSkillPresets.Count * allSkinPresets.Count);

            foreach (LoadoutSkillPreset skillPreset in allSkillPresets)
            {
                bool isCurrentSkills = ArrayUtils.SequenceEquals(currentSkillVariants, skillPreset.SkillVariants);

                foreach (LoadoutSkinPreset skinPreset in allSkinPresets)
                {
                    bool isCurrentSkin = currentSkinIndex == skinPreset.SkinIndex;

                    if (!isCurrentSkills || !isCurrentSkin)
                    {
                        LoadoutPreset preset = new LoadoutPreset(bodyIndex, skillPreset, skinPreset, 1f);
                        loadoutSelection.AddChoice(preset, preset.Weight);
                    }
                }
            }

            if (loadoutSelection.Count == 0)
            {
                changedAnySkill = false;
                changedSkin = false;
                return false;
            }

            LoadoutPreset loadoutPreset = loadoutSelection.GetRandom(rng);
            loadoutPreset.ApplyTo(loadout, out changedAnySkill, out changedSkin);
            return changedAnySkill || changedSkin;
        }

        static List<LoadoutSkillPreset> generateSkillPresets(NetworkUser networkUser, Loadout.BodyLoadoutManager.BodyInfo bodyInfo, uint[] currentSkillVariants)
        {
            List<LoadoutSkillPreset> allSkillPresets;
            if (_randomizeSkills.Value)
            {
                int skillSlotCount = bodyInfo.skillSlotCount;

                int[] skillVariantCount = new int[skillSlotCount];
                int presetCount = 1;
                for (int i = 0; i < skillSlotCount; i++)
                {
                    SkillFamily.Variant[] skillVariants = bodyInfo.prefabSkillSlots[i].skillFamily.variants;
                    skillVariantCount[i] = skillVariants.Length;

                    presetCount *= skillVariants.Length;
                }

                allSkillPresets = new List<LoadoutSkillPreset>(presetCount);

                for (int i = 0; i < presetCount; i++)
                {
                    uint[] skillVariants = new uint[skillSlotCount];

                    int completedCombinationCounts = 1;
                    for (int j = 0; j < skillSlotCount; j++)
                    {
                        int slotVariantCount = skillVariantCount[j];

                        skillVariants[j] = (uint)(i / completedCombinationCounts % slotVariantCount);
                        completedCombinationCounts *= slotVariantCount;
                    }

                    float weight = 1f;
                    for (int j = 0; j < skillVariants.Length; j++)
                    {
                        SkillFamily.Variant skillVariant = bodyInfo.prefabSkillSlots[j].skillFamily.variants[skillVariants[j]];
                        if (skillVariant.unlockableDef && networkUser && !networkUser.unlockables.Contains(skillVariant.unlockableDef))
                        {
                            weight = float.NegativeInfinity;
                            break;
                        }

                        if (skillVariants[j] == currentSkillVariants[j])
                        {
                            weight *= 0.9f;
                        }
                    }

                    if (weight > 0f)
                    {
                        LoadoutSkillPreset skillPreset = new LoadoutSkillPreset(skillVariants, weight);
                        allSkillPresets.Add(skillPreset);
                    }
                }
            }
            else
            {
                allSkillPresets = [new LoadoutSkillPreset(currentSkillVariants, 1f)];
            }

            return allSkillPresets;
        }

        static List<LoadoutSkinPreset> generateSkinPresets(BodyIndex bodyIndex, NetworkUser networkUser, uint currentSkinIndex)
        {
            List<LoadoutSkinPreset> allSkinPresets;
            if (_randomizeSkin.Value)
            {
                int bodySkinCount = SkinCatalog.GetBodySkinCount(bodyIndex);
                allSkinPresets = new List<LoadoutSkinPreset>(bodySkinCount);

                for (uint i = 0; i < bodySkinCount; i++)
                {
                    SkinDef skinDef = SkinCatalog.GetBodySkinDef(bodyIndex, (int)i);

                    if (skinDef && (!skinDef.unlockableDef || !networkUser || networkUser.unlockables.Contains(skinDef.unlockableDef)))
                    {
                        LoadoutSkinPreset skinPreset = new LoadoutSkinPreset(i, 1f);
                        allSkinPresets.Add(skinPreset);
                    }
                }
            }
            else
            {
                allSkinPresets = [new LoadoutSkinPreset(currentSkinIndex, 1f)];
            }

            return allSkinPresets;
        }

        record class LoadoutSkillPreset(uint[] SkillVariants, float Weight);

        record class LoadoutSkinPreset(uint SkinIndex, float Weight);

        class LoadoutPreset
        {
            public readonly BodyIndex BodyIndex;

            public readonly uint[] SkillVariants;

            public readonly uint? SkinIndex;

            public readonly float Weight;

            public LoadoutPreset(BodyIndex bodyIndex, LoadoutSkillPreset skillPreset, LoadoutSkinPreset skinPreset, float weight)
            {
                BodyIndex = bodyIndex;
                SkillVariants = skillPreset.SkillVariants;
                SkinIndex = skinPreset.SkinIndex;
                Weight = skillPreset.Weight * skinPreset.Weight * weight;
            }

            public void ApplyTo(Loadout loadout, out bool anySkillChanged, out bool skinChanged)
            {
                Loadout tmpLoadout = Loadout.RequestInstance();
                loadout.Copy(tmpLoadout);

                try
                {
                    anySkillChanged = false;
                    skinChanged = false;

                    if (SkillVariants != null)
                    {
                        for (int i = 0; i < SkillVariants.Length; i++)
                        {
                            uint currentSkillVariant = tmpLoadout.bodyLoadoutManager.GetSkillVariant(BodyIndex, i);
                            if (currentSkillVariant != SkillVariants[i])
                            {
                                anySkillChanged = true;
                                tmpLoadout.bodyLoadoutManager.SetSkillVariant(BodyIndex, i, SkillVariants[i]);
                            }
                        }
                    }

                    if (SkinIndex.HasValue)
                    {
                        uint currentSkinIndex = tmpLoadout.bodyLoadoutManager.GetSkinIndex(BodyIndex);
                        if (skinChanged = currentSkinIndex != SkinIndex)
                        {
                            tmpLoadout.bodyLoadoutManager.SetSkinIndex(BodyIndex, SkinIndex.Value);
                        }
                    }

                    if (anySkillChanged || skinChanged)
                    {
                        tmpLoadout.Copy(loadout);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error_NoCallerPrefix(ex);
                    anySkillChanged = false;
                    skinChanged = false;
                }
                finally
                {
                    Loadout.ReturnInstance(tmpLoadout);
                }
            }

            public override string ToString()
            {
                return $"[{string.Join(", ", SkillVariants)}] {SkinIndex} ({BodyCatalog.GetBodyName(BodyIndex)})";
            }
        }
    }
}
