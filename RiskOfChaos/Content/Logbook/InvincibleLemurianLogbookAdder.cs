using HG;
using R2API;
using RiskOfChaos.Components;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Stats;
using RoR2.UI;
using RoR2.UI.LogBook;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.Content.Logbook
{
    public static class InvincibleLemurianLogbookAdder
    {
        static CharacterBody _lemurianBodyPrefab;
        static GameObject _lemurianGlowModelPrefab;

        static CharacterBody _lemurianBruiserBodyPrefab;
        static GameObject _lemurianBruiserGlowModelPrefab;

        public readonly record struct LemurianStatCollection(UnlockableDef LogUnlockableDef, StatDef EncounteredStat, StatDef KilledByStat, StatDef KilledStat);

        public static readonly LemurianStatCollection LemurianStats = new LemurianStatCollection(
            Unlockables.InvincibleLemurianLogbook,
            StatDef.Register("invincibleLemuriansEncountered", StatRecordType.Sum, StatDataType.ULong, 0),
            StatDef.Register("invincibleLemuriansKilledBy", StatRecordType.Sum, StatDataType.ULong, 0),
            StatDef.Register("invincibleLemuriansKilled", StatRecordType.Sum, StatDataType.ULong, 0));

        public static readonly LemurianStatCollection ElderLemurianStats = new LemurianStatCollection(
            Unlockables.InvincibleLemurianElderLogbook,
            StatDef.Register("invincibleElderLemuriansEncountered", StatRecordType.Sum, StatDataType.ULong, 0),
            StatDef.Register("invincibleElderLemuriansKilledBy", StatRecordType.Sum, StatDataType.ULong, 0),
            StatDef.Register("invincibleElderLemuriansKilled", StatRecordType.Sum, StatDataType.ULong, 0));

        public static ref readonly LemurianStatCollection GetStatCollection(bool isElder)
        {
            return ref isElder ? ref ElderLemurianStats : ref LemurianStats;
        }

        [ConCommand(commandName = "roc_debug_reset_leonard_stats")]
        static void CCDebugResetLeonardStats(ConCommandArgs args)
        {
            LocalUser localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser == null)
                return;

            UserProfile userProfile = localUser.userProfile;
            if (userProfile == null)
                return;

            StatSheet statSheet = userProfile.statSheet;
            if (statSheet == null)
                return;

            const string ZERO_STRING = "0";

            statSheet.SetStatValueFromString(LemurianStats.EncounteredStat, ZERO_STRING);
            statSheet.SetStatValueFromString(LemurianStats.KilledByStat, ZERO_STRING);
            statSheet.SetStatValueFromString(LemurianStats.KilledStat, ZERO_STRING);

            statSheet.SetStatValueFromString(ElderLemurianStats.EncounteredStat, ZERO_STRING);
            statSheet.SetStatValueFromString(ElderLemurianStats.KilledByStat, ZERO_STRING);
            statSheet.SetStatValueFromString(ElderLemurianStats.KilledStat, ZERO_STRING);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            List<string> viewedViewables = userProfile.viewedViewables;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            viewedViewables.Remove("/Logbook/LOGBOOK_CATEGORY_MONSTER/INVINCIBLE_LEMURIAN_BODY_NAME");
            viewedViewables.Remove("/Logbook/LOGBOOK_CATEGORY_MONSTER/INVINCIBLE_LEMURIAN_ELDER_BODY_NAME");

            userProfile.RevokeUnlockable(Unlockables.InvincibleLemurianLogbook);
            userProfile.RevokeUnlockable(Unlockables.InvincibleLemurianElderLogbook);

            userProfile.RequestEventualSave();

            Debug.Log($"Reset Leonard stats for profile: {userProfile.name}");
        }

        [SystemInitializer]
        static void Init()
        {
            static GameObject createGlowModel(CharacterBody bodyPrefab)
            {
                if (!bodyPrefab)
                    return null;

                if (!bodyPrefab.TryGetComponent(out ModelLocator modelLocator))
                    return null;

                Transform modelPrefab = modelLocator.modelTransform;
                if (!modelPrefab)
                    return null;

                GameObject glowModelPrefab = modelPrefab.gameObject.InstantiateClone(modelPrefab.name + "Glow", false);

                ForceModelOverlay forceModelOverlay = glowModelPrefab.AddComponent<ForceModelOverlay>();
                forceModelOverlay.Overlay = CharacterModel.immuneMaterial;

                return glowModelPrefab;
            }

            GameObject lemurianPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lemurian/LemurianBody.prefab").WaitForCompletion();
            if (lemurianPrefab)
            {
                _lemurianBodyPrefab = lemurianPrefab.GetComponent<CharacterBody>();

                _lemurianGlowModelPrefab = createGlowModel(_lemurianBodyPrefab);
            }
            else
            {
                Log.Warning("Failed to load lemurian body prefab");
            }

            GameObject lemurianBruiserPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LemurianBruiser/LemurianBruiserBody.prefab").WaitForCompletion();
            if (lemurianBruiserPrefab)
            {
                _lemurianBruiserBodyPrefab = lemurianBruiserPrefab.GetComponent<CharacterBody>();

                _lemurianBruiserGlowModelPrefab = createGlowModel(_lemurianBruiserBodyPrefab);
            }
            else
            {
                Log.Warning("Failed to load elder lemurian body prefab");
            }

            GlobalEventManager.onCharacterDeathGlobal += report =>
            {
                if (!NetworkServer.active)
                    return;

                StatSheet victimStatSheet = PlayerStatsComponent.FindMasterStatSheet(report.victimMaster);
                if (victimStatSheet != null)
                {
                    if (report.attackerMaster)
                    {
                        Inventory attackerInventory = report.attackerMaster.inventory;
                        if (attackerInventory && attackerInventory.GetItemCount(Items.InvincibleLemurianMarker) > 0)
                        {
                            bool isElder = report.attackerBodyIndex == BodyCatalog.FindBodyIndex("LemurianBruiserBody");

                            ref readonly LemurianStatCollection lemurianStatCollection = ref GetStatCollection(isElder);

                            victimStatSheet.PushStatValue(lemurianStatCollection.KilledByStat, 1);

                            victimStatSheet.AddUnlockable(lemurianStatCollection.LogUnlockableDef);

#if DEBUG
                            Log.Debug($"Recorded Leonard player kill. victim={Util.GetBestMasterName(report.victimMaster)}, isElder={isElder}");
#endif
                        }
                    }
                }

                if (report.victimMaster)
                {
                    Inventory victimInventory = report.victimMaster.inventory;
                    if (victimInventory && victimInventory.GetItemCount(Items.InvincibleLemurianMarker) > 0)
                    {
                        bool isElder = report.victimBodyIndex == BodyCatalog.FindBodyIndex("LemurianBruiserBody");

                        ref readonly LemurianStatCollection lemurianStatCollection = ref GetStatCollection(isElder);

                        foreach (PlayerStatsComponent statsComponent in PlayerStatsComponent.instancesList)
                        {
                            statsComponent.currentStats.PushStatValue(lemurianStatCollection.KilledStat, 1);

                            statsComponent.currentStats.AddUnlockable(lemurianStatCollection.LogUnlockableDef);
                        }

#if DEBUG
                        Log.Debug($"Recorded Leonard death. attacker={Util.GetBestMasterName(report.attackerMaster)}, isElder={isElder}");
#endif
                    }
                }
            };

            On.RoR2.UI.LogBook.LogBookController.BuildMonsterEntries += LogBookController_BuildMonsterEntries;
        }

        static Entry[] LogBookController_BuildMonsterEntries(On.RoR2.UI.LogBook.LogBookController.orig_BuildMonsterEntries orig, Dictionary<ExpansionDef, bool> expansionAvailability)
        {
            Entry[] entries = orig(expansionAvailability);

            static EntryStatus getLeonardEntryStatus(UserProfile viewerProfile, in LemurianStatCollection statCollection)
            {
                StatSheet statSheet = viewerProfile.statSheet;

                if (statSheet.HasUnlockable(statCollection.LogUnlockableDef))
                {
                    return EntryStatus.Available;
                }
                else
                {
                    return EntryStatus.Unencountered;
                }
            }

            static void addLeonardBodyStats(PageBuilder builder, CharacterBody body)
            {
                const string INFINITY_STRING = "???";

                builder.AddSimpleTextPanel([
                    Language.GetStringFormatted("BODY_HEALTH_FORMAT", INFINITY_STRING),
                    Language.GetStringFormatted("BODY_DAMAGE_FORMAT", INFINITY_STRING),
                    Language.GetStringFormatted("BODY_MOVESPEED_FORMAT", body.baseMoveSpeed)
                ]);
            }

            static void addLeonardUserStats(PageBuilder builder, CharacterBody body, in LemurianStatCollection statCollection)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                StatSheet statSheet = builder.statSheet;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                ulong timesEncountered = statSheet.GetStatValueULong(statCollection.EncounteredStat);
                ulong timesKilledBy = statSheet.GetStatValueULong(statCollection.KilledByStat);
                ulong timesKilled = statSheet.GetStatValueULong(statCollection.KilledStat);

                builder.AddSimpleTextPanel([
                    Language.GetStringFormatted("LOGBOOK_INVINCIBLE_LEMURIAN_ENCOUNTERS_FORMAT", timesEncountered),
                    Language.GetStringFormatted("LOGBOOK_INVINCIBLE_LEMURIAN_DEATHS_FORMAT", timesKilledBy),
                    Language.GetStringFormatted("LOGBOOK_INVINCIBLE_LEMURIAN_KILLS_FORMAT", timesKilled)
                ]);
            }

            static void addLeonardLore(PageBuilder builder, string loreToken)
            {
                builder.AddNotesPanel(Language.GetString(loreToken));
            }

            TooltipContent getLeonardTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
            {
                if (status >= EntryStatus.Available)
                {
                    return new TooltipContent
                    {
                        overrideTitleText = entry.GetDisplayName(userProfile),
                        titleColor = entry.color,
                        bodyToken = string.Empty
                    };
                }
                else
                {
                    return new TooltipContent
                    {
                        titleToken = "UNIDENTIFIED",
                        titleColor = Color.gray,
                        bodyToken = "LOGBOOK_UNLOCK_ITEM_INVINCIBLE_LEMURIAN"
                    };
                }
            }

            if (_lemurianBodyPrefab)
            {
                int lemurianIndex = Array.FindIndex(entries, e => ReferenceEquals(e.extraData, _lemurianBodyPrefab));
                if (lemurianIndex != -1)
                {
                    Entry lemurianEntry = entries[lemurianIndex];

                    static EntryStatus getEntryStatus(in Entry entry, UserProfile viewerProfile)
                    {
                        return getLeonardEntryStatus(viewerProfile, LemurianStats);
                    }

                    static void pageBuilder(PageBuilder builder)
                    {
                        CharacterBody body = (CharacterBody)builder.entry.extraData;

                        addLeonardBodyStats(builder, body);
                        addLeonardUserStats(builder, body, LemurianStats);
                        addLeonardLore(builder, "INVINCIBLE_LEMURIAN_BODY_LORE");
                    }

                    ArrayUtils.ArrayInsert(ref entries, lemurianIndex + 1, new Entry
                    {
                        nameToken = "INVINCIBLE_LEMURIAN_BODY_NAME",
                        color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Interactable),
                        iconTexture = lemurianEntry.iconTexture,
                        extraData = lemurianEntry.extraData,
                        modelPrefab = _lemurianGlowModelPrefab ? _lemurianGlowModelPrefab : lemurianEntry.modelPrefab,
                        getStatusImplementation = getEntryStatus,
                        pageBuilderMethod = pageBuilder,
                        getTooltipContentImplementation = getLeonardTooltipContent,
                        bgTexture = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/texBossBGIcon.png").WaitForCompletion()
                    });
                }
            }

            if (_lemurianBruiserBodyPrefab)
            {
                int elderLemurianIndex = Array.FindIndex(entries, e => ReferenceEquals(e.extraData, _lemurianBruiserBodyPrefab));
                if (elderLemurianIndex != -1)
                {
                    Entry elderLemurianEntry = entries[elderLemurianIndex];

                    static EntryStatus getEntryStatus(in Entry entry, UserProfile viewerProfile)
                    {
                        return getLeonardEntryStatus(viewerProfile, ElderLemurianStats);
                    }

                    static void pageBuilder(PageBuilder builder)
                    {
                        CharacterBody body = (CharacterBody)builder.entry.extraData;

                        addLeonardBodyStats(builder, body);
                        addLeonardUserStats(builder, body, ElderLemurianStats);
                        addLeonardLore(builder, "INVINCIBLE_LEMURIAN_ELDER_BODY_LORE");
                    }

                    ArrayUtils.ArrayInsert(ref entries, elderLemurianIndex + 1, new Entry
                    {
                        nameToken = "INVINCIBLE_LEMURIAN_ELDER_BODY_NAME",
                        color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Interactable),
                        iconTexture = elderLemurianEntry.iconTexture,
                        extraData = elderLemurianEntry.extraData,
                        modelPrefab = _lemurianBruiserGlowModelPrefab ? _lemurianBruiserGlowModelPrefab : elderLemurianEntry.modelPrefab,
                        getStatusImplementation = getEntryStatus,
                        pageBuilderMethod = pageBuilder,
                        getTooltipContentImplementation = getLeonardTooltipContent,
                        bgTexture = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/texBossBGIcon.png").WaitForCompletion()
                    });
                }
            }

            return entries;
        }
    }
}
