using HG;
using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Stats;
using RoR2.UI;
using RoR2.UI.LogBook;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Content.Logbook
{
    public static class InvincibleLemurianLogbookAdder
    {
        static CharacterBody _lemurianBodyPrefab;
        static GameObject _lemurianGlowModelPrefab;

        static CharacterBody _lemurianBruiserBodyPrefab;
        static GameObject _lemurianBruiserGlowModelPrefab;

        public record LemurianStatCollection(UnlockableDef LogUnlockableDef, StatDef EncounteredStat, StatDef KilledByStat, StatDef KilledStat);

        static LemurianStatCollection _lemurianStats;

        static LemurianStatCollection _elderLemurianStats;

        public static LemurianStatCollection GetStatCollection(BodyIndex bodyIndex)
        {
            if (bodyIndex == BodyIndex.None)
                return null;

            if (bodyIndex == BodyCatalog.FindBodyIndex("InvincibleLemurianBody"))
                return _lemurianStats;

            if (bodyIndex == BodyCatalog.FindBodyIndex("InvincibleLemurianBruiserBody"))
                return _elderLemurianStats;

            return null;
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
            if (statSheet != null)
            {
                const string ZERO_STRING = "0";

                statSheet.SetStatValueFromString(_lemurianStats.EncounteredStat, ZERO_STRING);
                statSheet.SetStatValueFromString(_lemurianStats.KilledByStat, ZERO_STRING);
                statSheet.SetStatValueFromString(_lemurianStats.KilledStat, ZERO_STRING);

                statSheet.SetStatValueFromString(_elderLemurianStats.EncounteredStat, ZERO_STRING);
                statSheet.SetStatValueFromString(_elderLemurianStats.KilledByStat, ZERO_STRING);
                statSheet.SetStatValueFromString(_elderLemurianStats.KilledStat, ZERO_STRING);
            }

            List<string> viewedViewables = userProfile.viewedViewables;

            viewedViewables.Remove("/Logbook/LOGBOOK_CATEGORY_MONSTER/INVINCIBLE_LEMURIAN_BODY_NAME");
            viewedViewables.Remove("/Logbook/LOGBOOK_CATEGORY_MONSTER/INVINCIBLE_LEMURIAN_ELDER_BODY_NAME");

            userProfile.RevokeUnlockable(_lemurianStats.LogUnlockableDef);
            userProfile.RevokeUnlockable(_elderLemurianStats.LogUnlockableDef);

            userProfile.RequestEventualSave();

            Debug.Log($"Reset Leonard stats for profile: {userProfile.name}");
        }

        [ContentInitializer]
        static IEnumerator LoadContent(LocalPrefabAssetCollection localPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(2);

            static GameObject createGlowModel(CharacterBody bodyPrefab)
            {
                if (!bodyPrefab)
                    return null;

                if (!bodyPrefab.TryGetComponent(out ModelLocator modelLocator))
                    return null;

                Transform modelPrefab = modelLocator.modelTransform;
                if (!modelPrefab)
                    return null;

                GameObject glowModelPrefab = modelPrefab.gameObject.InstantiatePrefab(modelPrefab.name + "Glow");

                PersistentOverlayController overlayController = glowModelPrefab.AddComponent<PersistentOverlayController>();
                overlayController.OverlayMaterialReference = new AssetReferenceT<Material>(AddressableGuids.RoR2_Base_Common_matImmune_mat);

                return glowModelPrefab;
            }

            AsyncOperationHandle<GameObject> lemurianBodyLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Lemurian_LemurianBody_prefab, AsyncReferenceHandleUnloadType.Preload);
            lemurianBodyLoad.OnSuccess(lemurianBody =>
            {
                _lemurianBodyPrefab = lemurianBody.GetComponent<CharacterBody>();

                _lemurianGlowModelPrefab = createGlowModel(_lemurianBodyPrefab);

                localPrefabs.Add(_lemurianGlowModelPrefab);
            });

            asyncOperations.Add(lemurianBodyLoad);

            AsyncOperationHandle<GameObject> elderLemurianBodyLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_LemurianBruiser_LemurianBruiserBody_prefab, AsyncReferenceHandleUnloadType.Preload);
            elderLemurianBodyLoad.OnSuccess(elderLemurianBody =>
            {
                _lemurianBruiserBodyPrefab = elderLemurianBody.GetComponent<CharacterBody>();

                _lemurianBruiserGlowModelPrefab = createGlowModel(_lemurianBruiserBodyPrefab);

                localPrefabs.Add(_lemurianBruiserGlowModelPrefab);
            });

            asyncOperations.Add(elderLemurianBodyLoad);

            yield return asyncOperations.WaitForAllLoaded();
        }

        [SystemInitializer]
        static void Init()
        {
            _lemurianStats = new LemurianStatCollection(
                RoCContent.Unlockables.InvincibleLemurianLog,
                StatDef.Register("invincibleLemuriansEncountered", StatRecordType.Sum, StatDataType.ULong, 0),
                StatDef.Register("invincibleLemuriansKilledBy", StatRecordType.Sum, StatDataType.ULong, 0),
                StatDef.Register("invincibleLemuriansKilled", StatRecordType.Sum, StatDataType.ULong, 0));

            _elderLemurianStats = new LemurianStatCollection(
                RoCContent.Unlockables.InvincibleLemurianElderLog,
                StatDef.Register("invincibleElderLemuriansEncountered", StatRecordType.Sum, StatDataType.ULong, 0),
                StatDef.Register("invincibleElderLemuriansKilledBy", StatRecordType.Sum, StatDataType.ULong, 0),
                StatDef.Register("invincibleElderLemuriansKilled", StatRecordType.Sum, StatDataType.ULong, 0));

            On.RoR2.UI.LogBook.LogBookController.BuildMonsterEntries += LogBookController_BuildMonsterEntries;
        }

        static Entry[] LogBookController_BuildMonsterEntries(On.RoR2.UI.LogBook.LogBookController.orig_BuildMonsterEntries orig, Dictionary<ExpansionDef, bool> expansionAvailability)
        {
            Entry[] entries = orig(expansionAvailability);

            static EntryStatus getLeonardEntryStatus(UserProfile viewerProfile, LemurianStatCollection statCollection)
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

            static void addLeonardUserStats(PageBuilder builder, CharacterBody body, LemurianStatCollection statCollection)
            {
                StatSheet statSheet = builder.statSheet;

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

            static TooltipContent getLeonardTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
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

            Entry insertInvincibleLemurianEntry(CharacterBody lemurianPrefab, CharacterBody invincibleLemurianPrefab, GameObject modelPrefab, Entry.GetStatusDelegate getStatus, Action<PageBuilder> pageBuilder, Entry.GetTooltipContentDelegate getTooltipContent)
            {
                // Gearbox fucked up and put devoted lemurians in the logbook, so the instance we have is actually not in the entries array at all
                // So hacky name token comparison it is!
                int lemurianIndex = Array.FindIndex(entries, e => e.extraData is CharacterBody body && body.baseNameToken == lemurianPrefab.baseNameToken);
                int invincibleLemurianInsertIndex = lemurianIndex + 1;
                if (lemurianIndex == -1)
                {
                    invincibleLemurianInsertIndex = entries.Length;

                    for (int i = 0; i < entries.Length; i++)
                    {
                        if (entries[i].extraData is CharacterBody body)
                        {
                            if (body.baseMaxHealth > invincibleLemurianPrefab.baseMaxHealth)
                            {
                                invincibleLemurianInsertIndex = i;
                                break;
                            }
                        }
                    }
                }

                if (!modelPrefab)
                {
                    if (invincibleLemurianPrefab.TryGetComponent(out ModelLocator modelLocator) && modelLocator.modelTransform)
                    {
                        modelPrefab = modelLocator.modelTransform.gameObject;
                    }
                }

#pragma warning disable CS0618 // Type or member is obsolete
                Entry entry = new Entry
                {
                    nameToken = invincibleLemurianPrefab.baseNameToken,
                    color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Interactable),
                    iconTexture = invincibleLemurianPrefab.portraitIcon,
                    extraData = invincibleLemurianPrefab,
                    modelPrefab = modelPrefab,
                    getStatusImplementation = getStatus,
                    pageBuilderMethod = pageBuilder,
                    getTooltipContentImplementation = getTooltipContent,
                    bgTexture = AddressableUtil.LoadAssetAsync<Texture2D>(AddressableGuids.RoR2_Base_Common_texBossBGIcon_png).WaitForCompletion()
                };
#pragma warning restore CS0618 // Type or member is obsolete

                ArrayUtils.ArrayInsert(ref entries, invincibleLemurianInsertIndex, entry);

                return entry;
            }

            GameObject invincibleLemurianBodyPrefabObj = BodyCatalog.FindBodyPrefab("InvincibleLemurianBody");
            if (_lemurianBodyPrefab && invincibleLemurianBodyPrefabObj && invincibleLemurianBodyPrefabObj.TryGetComponent(out CharacterBody invincibleLemurianBody))
            {
                static EntryStatus getEntryStatus(in Entry entry, UserProfile viewerProfile)
                {
                    return getLeonardEntryStatus(viewerProfile, _lemurianStats);
                }

                static void pageBuilder(PageBuilder builder)
                {
                    CharacterBody body = (CharacterBody)builder.entry.extraData;

                    addLeonardBodyStats(builder, body);
                    addLeonardUserStats(builder, body, _lemurianStats);
                    addLeonardLore(builder, "INVINCIBLE_LEMURIAN_BODY_LORE");
                }

                insertInvincibleLemurianEntry(_lemurianBodyPrefab, invincibleLemurianBody, _lemurianGlowModelPrefab, getEntryStatus, pageBuilder, getLeonardTooltipContent);
            }

            GameObject invincibleLemurianBruiserBodyPrefabObj = BodyCatalog.FindBodyPrefab("InvincibleLemurianBruiserBody");
            if (_lemurianBruiserBodyPrefab && invincibleLemurianBruiserBodyPrefabObj && invincibleLemurianBruiserBodyPrefabObj.TryGetComponent(out CharacterBody invincibleLemurianBruiserBody))
            {
                static EntryStatus getEntryStatus(in Entry entry, UserProfile viewerProfile)
                {
                    return getLeonardEntryStatus(viewerProfile, _elderLemurianStats);
                }

                static void pageBuilder(PageBuilder builder)
                {
                    CharacterBody body = (CharacterBody)builder.entry.extraData;

                    addLeonardBodyStats(builder, body);
                    addLeonardUserStats(builder, body, _elderLemurianStats);
                    addLeonardLore(builder, "INVINCIBLE_LEMURIAN_ELDER_BODY_LORE");
                }

                insertInvincibleLemurianEntry(_lemurianBruiserBodyPrefab, invincibleLemurianBruiserBody, _lemurianBruiserGlowModelPrefab, getEntryStatus, pageBuilder, getLeonardTooltipContent);
            }

            return entries;
        }
    }
}
