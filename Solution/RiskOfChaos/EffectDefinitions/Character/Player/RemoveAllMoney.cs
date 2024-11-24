using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("remove_all_money", DefaultSelectionWeight = 0.6f)]
    public sealed class RemoveAllMoney : MonoBehaviour
    {
        const int BURST_COUNT = 8;
        const int TARGET_CONVERT_TIME_SECONDS = 2;
        const float BURST_INTERVAL = 1f / (BURST_COUNT / (float)TARGET_CONVERT_TIME_SECONDS);

        readonly Dictionary<GameObject, uint> _expBurstSizes = [];

        float _expBurstTimer;

        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectComponent.EffectDestructionHandledByComponent = true;
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;

            _expBurstTimer -= Time.fixedDeltaTime;
            if (_expBurstTimer <= 0f)
            {
                bool grantedAnyExperience = false;

                List<CharacterMaster> validMasters = new List<CharacterMaster>(CharacterMaster.readOnlyInstancesList.Count);
                Dictionary<TeamIndex, int> teamMemberCountsByTeamIndex = new Dictionary<TeamIndex, int>((int)TeamIndex.Count);

                foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
                {
                    if (master.IsPlayerOrPlayerAlly() && (master.playerCharacterMasterController || master.money > 0))
                    {
                        validMasters.Add(master);

                        if (!teamMemberCountsByTeamIndex.TryGetValue(master.teamIndex, out int teamMemberCount))
                        {
                            teamMemberCount = 0;
                        }

                        teamMemberCountsByTeamIndex[master.teamIndex] = teamMemberCount + 1;
                    }
                }

                _expBurstSizes.EnsureCapacity(validMasters.Count);

                foreach (CharacterMaster master in validMasters)
                {
                    if (!_expBurstSizes.TryGetValue(master.gameObject, out uint burstSize))
                    {
                        burstSize = (uint)Mathf.CeilToInt(master.money / (float)BURST_COUNT);
                        _expBurstSizes.Add(master.gameObject, burstSize);
                    }

                    if (burstSize > master.money)
                    {
                        burstSize = master.money;
                    }

                    master.money -= burstSize;

                    float teamMemberCountPenalty = 1f;
                    if (teamMemberCountsByTeamIndex.TryGetValue(master.teamIndex, out int teamMemberCount))
                    {
                        teamMemberCountPenalty = teamMemberCount;
                    }

                    ulong experience = (ulong)(burstSize / 2f / teamMemberCountPenalty);
                    if (experience > 0)
                    {
                        if (burstSize > 0U)
                        {
                            grantedAnyExperience = true;
                        }

                        CharacterBody body = master.GetBody();
                        if (body)
                        {
                            ExperienceManager.instance.AwardExperience(body.corePosition, body, experience);
                        }
                        else
                        {
                            TeamManager.instance.GiveTeamExperience(master.teamIndex, experience);
                        }
                    }
                }

                if (grantedAnyExperience)
                {
                    _expBurstTimer = BURST_INTERVAL;
                }
                else if (_expBurstTimer < -0.5f && _effectComponent.TimeStarted.TimeSince > 1f)
                {
                    _effectComponent.RetireEffect();
                }
            }
        }
    }
}
