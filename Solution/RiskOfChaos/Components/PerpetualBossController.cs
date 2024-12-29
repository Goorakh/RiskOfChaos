using RiskOfChaos.Content;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class PerpetualBossController : MonoBehaviour
    {
        public CombatDirector BossDirector;

        public float CreditMultiplier = 1f;

        public float BossDelay = 3.5f;

        CombatSquad _currentCombatSquad;

        float _nextBossSpawnTimer;

        void OnEnable()
        {
            if (!NetworkServer.active)
                return;

            if (BossDirector)
            {
                BossDirector.enabled = true;
            }

            if (_currentCombatSquad && !_currentCombatSquad.defeatedServer)
            {
                _currentCombatSquad.onDefeatedServer += onBossDefeatedServer;
            }
            else
            {
                spawnNextBoss();
            }
        }

        void OnDisable()
        {
            if (BossDirector)
            {
                BossDirector.enabled = false;
            }

            if (_currentCombatSquad)
            {
                _currentCombatSquad.onDefeatedServer -= onBossDefeatedServer;
            }
        }

        void FixedUpdate()
        {
            if (_nextBossSpawnTimer > 0f)
            {
                _nextBossSpawnTimer -= Time.fixedDeltaTime;
                if (_nextBossSpawnTimer <= 0f)
                {
                    spawnNextBoss();
                }
            }
        }

        void spawnNextBoss()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            if (_currentCombatSquad)
            {
                _currentCombatSquad.onDefeatedServer -= onBossDefeatedServer;
                Destroy(_currentCombatSquad.gameObject);
            }

            GameObject bossCombatSquadObj = Instantiate(RoCContent.NetworkedPrefabs.BossCombatSquadNoReward);

            _currentCombatSquad = bossCombatSquadObj.GetComponent<CombatSquad>();
            _currentCombatSquad.onDefeatedServer += onBossDefeatedServer;

            NetworkServer.Spawn(bossCombatSquadObj);

            BossDirector.combatSquad = _currentCombatSquad;
            BossDirector.monsterCredit = (int)(600f * Mathf.Pow(Run.instance.compensatedDifficultyCoefficient, 0.5f) * CreditMultiplier);
            BossDirector.SetNextSpawnAsBoss();
        }

        void onBossDefeatedServer()
        {
            _nextBossSpawnTimer = Mathf.Max(_nextBossSpawnTimer, BossDelay);
        }
    }
}
