using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    /*
    [ChaosEffect("randomize_loadout", DefaultSelectionWeight = 0.7f)]
    public sealed class RandomizeLoadout : NetworkBehaviour
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

        ChaosEffectComponent _effectComponent;

        ulong _rngSeed;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rngSeed = _effectComponent.Rng.nextUlong;
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                if (!master.IsDeadAndOutOfLivesServer() || master.playerCharacterMasterController)
                {
                    Xoroshiro128Plus rng = new Xoroshiro128Plus(_rngSeed);

                    try
                    {
                        tryRandomizeLoadout(master, rng);
                    }
                    catch (Exception e)
                    {
                        Log.Error_NoCallerPrefix($"Failed to randomize loadout for {Util.GetBestMasterName(master)}: {e}");
                    }
                }
            }
        }

        void tryRandomizeLoadout(CharacterMaster master, Xoroshiro128Plus rng)
        {
            CharacterBody body = master.GetBody();

            BodyIndex currentBodyIndex = BodyIndex.None;
            if (body)
            {
                currentBodyIndex = body.bodyIndex;
            }
            else if (master.bodyPrefab)
            {
                currentBodyIndex = BodyCatalog.FindBodyIndex(master.bodyPrefab);
            }
            else if (master.playerCharacterMasterController && master.playerCharacterMasterController.networkUser)
            {
                currentBodyIndex = master.playerCharacterMasterController.networkUser.bodyIndexPreference;
            }

            if (currentBodyIndex == BodyIndex.None)
                return;

            Loadout loadout = master.loadout;

            bool anyChanges = false;
            bool changedCurrentBody = false;

            List<BodyIndex> bodyIndicesToRandomize = [currentBodyIndex];

            if (master.playerCharacterMasterController)
            {
                bodyIndicesToRandomize.EnsureCapacity(bodyIndicesToRandomize.Count + SurvivorCatalog.survivorCount);
                foreach (SurvivorDef survivor in SurvivorCatalog.allSurvivorDefs)
                {
                    BodyIndex survivorBodyIndex = BodyCatalog.FindBodyIndex(survivor.bodyPrefab);
                    if (survivorBodyIndex != BodyIndex.None && !bodyIndicesToRandomize.Contains(survivorBodyIndex))
                    {
                        bodyIndicesToRandomize.Add(survivorBodyIndex);
                    }
                }
            }

            foreach (BodyIndex bodyIndex in bodyIndicesToRandomize)
            {
                tryRandomizeLoadoutForBodyIndex(master, loadout, bodyIndex, rng, out bool changedAnySkill, out bool changedSkin);
                if (changedAnySkill || changedSkin)
                {
                    anyChanges = true;

                    if (body && bodyIndex == body.bodyIndex)
                    {
                        changedCurrentBody = true;
                    }
                }
            }

            if (anyChanges)
            {
                // Set dirty bit
                master.SetLoadoutServer(loadout);

                if (changedCurrentBody && body)
                {
                    body.SetLoadoutServer(loadout);

                    Loadout.BodyLoadoutManager.BodyInfo bodyInfo = Loadout.BodyLoadoutManager.allBodyInfos[(int)body.bodyIndex];

                    uint[] skillVariants = new uint[bodyInfo.skillSlotCount];
                    for (int i = 0; i < skillVariants.Length; i++)
                    {
                        skillVariants[i] = loadout.bodyLoadoutManager.GetSkillVariant(body.bodyIndex, i);
                    }

                    uint skinIndex = loadout.bodyLoadoutManager.GetSkinIndex(body.bodyIndex);

                    updateLoadout(body, skillVariants, skinIndex);
                    RpcUpdateLoadout(body.gameObject, skillVariants, skinIndex);
                }
            }
        }

        [ClientRpc]
        void RpcUpdateLoadout(GameObject bodyObject, uint[] skillVariantIndices, uint skinIndex)
        {
            if (NetworkServer.active)
                return;

            if (!bodyObject)
                return;

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();

            updateLoadout(body, skillVariantIndices, skinIndex);
        }

        void updateLoadout(CharacterBody body, uint[] skillVariantIndices, uint skinIndex)
        {
            if (!body)
                return;

            // if we don't have authority over the body, the skill replacements will already be handled by SkillLocator.OnDeserialize
            if (body.hasEffectiveAuthority)
            {
                SkillLocator skillLocator = body.skillLocator;

                for (int i = 0; i < skillLocator.allSkills.Length; i++)
                {
                    if (i >= skillVariantIndices.Length)
                    {
                        Log.Warning($"({Util.GetBestBodyName(body.gameObject)}) Skill locator has more skills than are defined in the network message: defined={skillLocator.allSkills.Length}, received={skillVariantIndices.Length}");
                        break;
                    }

                    GenericSkill genericSkill = skillLocator.allSkills[i];

                    SkillFamily.Variant[] skillVariants = genericSkill.skillFamily.variants;

                    uint skillVariantIndex = skillVariantIndices[i];
                    if (skillVariantIndex >= skillVariants.Length)
                    {
                        Log.Warning($"({Util.GetBestBodyName(body.gameObject)}) Skill variant {SkillCatalog.GetSkillFamilyName(genericSkill.skillFamily.catalogIndex)} index out of range! expected={skillVariants.Length}, received={skillVariantIndex}");
                        continue;
                    }

                    SkillDef newSkillDef = skillVariants[skillVariantIndex].skillDef;
                    if (genericSkill.baseSkill != newSkillDef)
                    {
                        genericSkill.SetBaseSkill(newSkillDef);
                    }
                }
            }

            ModelLocator modelLocator = body.modelLocator;
            if (modelLocator)
            {
                Transform modelTransform = modelLocator.modelTransform;
                if (modelTransform && modelTransform.TryGetComponent(out ModelSkinController modelSkinController))
                {
                    if (modelSkinController.currentSkinIndex != skinIndex)
                    {
                        modelSkinController.StartCoroutine(modelSkinController.ApplySkinAsync(ClampedConversion.Int32(skinIndex), AsyncReferenceHandleUnloadType.OnRunEnd));
                    }
                }
            }
        }

        static void tryRandomizeLoadoutForBodyIndex(CharacterMaster master, Loadout loadout, BodyIndex bodyIndex, Xoroshiro128Plus rng, out bool changedAnySkill, out bool changedSkin)
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
                return;
            }

            LoadoutPreset loadoutPreset = loadoutSelection.GetRandom(rng);
            loadoutPreset.ApplyTo(loadout, out changedAnySkill, out changedSkin);
        }

        static List<LoadoutSkillPreset> generateSkillPresets(NetworkUser networkUser, Loadout.BodyLoadoutManager.BodyInfo bodyInfo, uint[] currentSkillVariants)
        {
            if (!_randomizeSkills.Value)
                return [new LoadoutSkillPreset(currentSkillVariants, 1f)];

            int skillSlotCount = bodyInfo.skillSlotCount;

            int[] skillVariantCount = new int[skillSlotCount];
            int presetCount = 1;
            for (int i = 0; i < skillSlotCount; i++)
            {
                SkillFamily.Variant[] skillVariants = bodyInfo.prefabSkillSlots[i].skillFamily.variants;
                skillVariantCount[i] = skillVariants.Length;

                presetCount *= skillVariants.Length;
            }

            List<LoadoutSkillPreset> allSkillPresets = new List<LoadoutSkillPreset>(presetCount);

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

            return allSkillPresets;
        }

        static List<LoadoutSkinPreset> generateSkinPresets(BodyIndex bodyIndex, NetworkUser networkUser, uint currentSkinIndex)
        {
            if (!_randomizeSkin.Value)
                return [new LoadoutSkinPreset(currentSkinIndex, 1f)];

            int bodySkinCount = SkinCatalog.GetBodySkinCount(bodyIndex);
            List<LoadoutSkinPreset> allSkinPresets = new List<LoadoutSkinPreset>(bodySkinCount);

            for (uint i = 0; i < bodySkinCount; i++)
            {
                SkinDef skinDef = SkinCatalog.GetBodySkinDef(bodyIndex, (int)i);
                if (!skinDef)
                    continue;

                if (skinDef.unlockableDef && networkUser && !networkUser.unlockables.Contains(skinDef.unlockableDef))
                    continue;
                
                LoadoutSkinPreset skinPreset = new LoadoutSkinPreset(i, 1f);
                allSkinPresets.Add(skinPreset);
            }

            return allSkinPresets;
        }

        record class LoadoutSkillPreset(uint[] SkillVariants, float Weight);

        record class LoadoutSkinPreset(uint SkinIndex, float Weight);

        class LoadoutPreset
        {
            public readonly BodyIndex BodyIndex;

            public readonly uint[] SkillVariants;

            public readonly uint SkinIndex;

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

                    uint currentSkinIndex = tmpLoadout.bodyLoadoutManager.GetSkinIndex(BodyIndex);
                    if (currentSkinIndex != SkinIndex)
                    {
                        skinChanged = true;
                        tmpLoadout.bodyLoadoutManager.SetSkinIndex(BodyIndex, SkinIndex);
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
    */
}
