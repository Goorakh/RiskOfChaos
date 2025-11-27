using RiskOfChaos.Content;
using RiskOfChaos.Content.Logbook;
using RoR2;
using RoR2.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public sealed class InvincibleLemurianController : MonoBehaviour, IOnKilledServerReceiver, IOnKilledOtherServerReceiver
    {
        CharacterBody _body;

        public InvincibleLemurianLogbookAdder.LemurianStatCollection StatCollection { get; private set; }

        void Awake()
        {
            _body = GetComponent<CharacterBody>();
            StatCollection = InvincibleLemurianLogbookAdder.GetStatCollection(_body.bodyIndex);
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                Inventory inventory = _body.inventory;
                if (inventory)
                {
                    if (inventory.GetItemCountPermanent(RoCContent.Items.InvincibleLemurianMarker) == 0)
                    {
                        inventory.GiveItemPermanent(RoCContent.Items.InvincibleLemurianMarker);

                        InvincibleLemurianLogbookAdder.LemurianStatCollection statCollection = StatCollection;
                        if (statCollection != null)
                        {
                            foreach (NetworkUser networkUser in NetworkUser.readOnlyInstancesList)
                            {
                                if (networkUser.isParticipating)
                                {
                                    PlayerStatsComponent statsComponent = networkUser.masterPlayerStatsComponent;
                                    if (statsComponent)
                                    {
                                        statsComponent.currentStats.PushStatValue(statCollection.EncounteredStat, 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void IOnKilledServerReceiver.OnKilledServer(DamageReport damageReport)
        {
            InvincibleLemurianLogbookAdder.LemurianStatCollection lemurianStatCollection = StatCollection;
            if (lemurianStatCollection == null)
                return;

            foreach (NetworkUser networkUser in NetworkUser.readOnlyInstancesList)
            {
                if (networkUser.isParticipating)
                {
                    PlayerStatsComponent statsComponent = networkUser.masterPlayerStatsComponent;
                    if (statsComponent)
                    {
                        statsComponent.currentStats.PushStatValue(lemurianStatCollection.KilledStat, 1);
                    }
                }
            }

            if (Run.instance)
            {
                Run.instance.GrantUnlockToAllParticipatingPlayers(lemurianStatCollection.LogUnlockableDef);
            }

            Log.Debug($"Recorded Leonard death. attacker={Util.GetBestMasterName(damageReport.attackerMaster)}");
        }

        void IOnKilledOtherServerReceiver.OnKilledOtherServer(DamageReport damageReport)
        {
            InvincibleLemurianLogbookAdder.LemurianStatCollection lemurianStatCollection = StatCollection;
            if (lemurianStatCollection == null)
                return;

            if (damageReport.victimMaster)
            {
                PlayerCharacterMasterController victimPlayerMasterController = damageReport.victimMaster.playerCharacterMasterController;
                if (victimPlayerMasterController)
                {
                    NetworkUser victimNetworkUser = victimPlayerMasterController.networkUser;
                    if (victimNetworkUser)
                    {
                        PlayerStatsComponent victimStatsComponent = victimNetworkUser.masterPlayerStatsComponent;
                        if (victimStatsComponent)
                        {
                            victimStatsComponent.currentStats.PushStatValue(lemurianStatCollection.KilledByStat, 1);
                        }

                        if (!victimNetworkUser.unlockables.Contains(lemurianStatCollection.LogUnlockableDef))
                        {
                            victimNetworkUser.ServerHandleUnlock(lemurianStatCollection.LogUnlockableDef);
                        }
                    }
                }

                Log.Debug($"Recorded Leonard player kill. victim={Util.GetBestMasterName(damageReport.victimMaster)}");
            }
        }
    }
}
