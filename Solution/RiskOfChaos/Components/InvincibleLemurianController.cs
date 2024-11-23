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
                    if (inventory.GetItemCount(RoCContent.Items.InvincibleLemurianMarker) == 0)
                    {
                        inventory.GiveItem(RoCContent.Items.InvincibleLemurianMarker);

                        InvincibleLemurianLogbookAdder.LemurianStatCollection statCollection = StatCollection;
                        if (statCollection != null)
                        {
                            foreach (PlayerStatsComponent statsComponent in PlayerStatsComponent.instancesList)
                            {
                                statsComponent.currentStats.PushStatValue(statCollection.EncounteredStat, 1);
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

            foreach (PlayerStatsComponent statsComponent in PlayerStatsComponent.instancesList)
            {
                statsComponent.currentStats.PushStatValue(lemurianStatCollection.KilledStat, 1);

                statsComponent.currentStats.AddUnlockable(lemurianStatCollection.LogUnlockableDef);
            }

            Log.Debug($"Recorded Leonard death. attacker={Util.GetBestMasterName(damageReport.attackerMaster)}");
        }

        void IOnKilledOtherServerReceiver.OnKilledOtherServer(DamageReport damageReport)
        {
            InvincibleLemurianLogbookAdder.LemurianStatCollection lemurianStatCollection = StatCollection;
            if (lemurianStatCollection == null)
                return;

            StatSheet victimStatSheet = PlayerStatsComponent.FindMasterStatSheet(damageReport.victimMaster);
            if (victimStatSheet == null)
                return;

            victimStatSheet.PushStatValue(lemurianStatCollection.KilledByStat, 1);

            victimStatSheet.AddUnlockable(lemurianStatCollection.LogUnlockableDef);

            Log.Debug($"Recorded Leonard player kill. victim={Util.GetBestMasterName(damageReport.victimMaster)}");
        }
    }
}
