using RiskOfChaos.Content;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components
{
    public class PerpetualBossController : MonoBehaviour
    {
        public CombatDirector BossDirector;

        public float BaseCreditMultiplier = 1f;

        public float CreditMultiplierPerWave = 0f;

        public float BossDelay = 3.5f;

        public float CurrentCreditMultiplier => BaseCreditMultiplier + (CreditMultiplierPerWave * _waveCount);

        int _waveCount;

        CombatSquad _currentCombatSquad;

        float _nextBossSpawnTimer;

        void OnEnable()
        {
            if (NetworkServer.active)
            {
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
            if (NetworkServer.active)
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

            float difficultyCoefficient = Run.instance ? Run.instance.compensatedDifficultyCoefficient : 1f;

            BossDirector.monsterCredit = (int)(600f * Mathf.Pow(difficultyCoefficient, 0.5f) * CurrentCreditMultiplier);
            BossDirector.combatSquad = _currentCombatSquad;
            BossDirector.SetNextSpawnAsBoss();
        }

        void onBossDefeatedServer()
        {
            _waveCount++;
            _nextBossSpawnTimer = Mathf.Max(_nextBossSpawnTimer, BossDelay);
        }
    }
}
