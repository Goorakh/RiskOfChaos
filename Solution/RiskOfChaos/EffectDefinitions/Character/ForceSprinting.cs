using RiskOfChaos.Collections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectUtils.Character.AllSkillsAgile;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("force_sprinting", 60f, AllowDuplicates = false)]
    [IncompatibleEffects(typeof(DisableSprinting))]
    public sealed class ForceSprinting : MonoBehaviour
    {
        readonly ClearingObjectList<ForceNonZeroMoveDirection> _forceMoveDirectionControllers = [];

        void Start()
        {
            OverrideSkillsAgile.AllSkillsAgileCount++;

            _forceMoveDirectionControllers.EnsureCapacity(CharacterBody.readOnlyInstancesList.Count);
            CharacterBody.readOnlyInstancesList.TryDo(tryAddForceMovementComponent, FormatUtils.GetBestBodyName);
            CharacterBody.onBodyStartGlobal += onBodyStartGlobal;

            SetIsSprintingOverride.OverrideCharacterSprinting += overrideSprint;
        }

        void OnDestroy()
        {
            OverrideSkillsAgile.AllSkillsAgileCount--;

            CharacterBody.onBodyStartGlobal -= onBodyStartGlobal;

            SetIsSprintingOverride.OverrideCharacterSprinting -= overrideSprint;

            _forceMoveDirectionControllers.ClearAndDispose(true);
        }

        void onBodyStartGlobal(CharacterBody body)
        {
            tryAddForceMovementComponent(body);
        }

        void tryAddForceMovementComponent(CharacterBody body)
        {
            if (!body.inputBank || !body.hasEffectiveAuthority)
                return;

            ForceNonZeroMoveDirection forceNonZeroMoveDirection = body.gameObject.AddComponent<ForceNonZeroMoveDirection>();
            _forceMoveDirectionControllers.Add(forceNonZeroMoveDirection);

            Log.Debug($"Added component to {FormatUtils.GetBestBodyName(body)}");
        }

        static void overrideSprint(CharacterBody body, ref bool isSprinting)
        {
            isSprinting = true;
        }

        class ForceNonZeroMoveDirection : MonoBehaviour
        {
            CharacterBody _body;
            InputBankTest _inputBank;

            Vector3 _lastNonZeroMoveDirection;

            void Awake()
            {
                _inputBank = GetComponent<InputBankTest>();
                if (!_inputBank)
                {
                    Log.Error("Missing InputBankTest component");
                    enabled = false;
                    return;
                }

                _body = GetComponent<CharacterBody>();
                if (!_body)
                {
                    Log.Error("Missing CharacterBody component");
                    enabled = false;
                    return;
                }
            }

            void Start()
            {
                Vector3 moveDir = _inputBank.aimDirection;
                if (!_body.isFlying)
                {
                    Vector3 groundMoveDir = Vector3.ProjectOnPlane(moveDir, Vector3.up);
                    if (groundMoveDir.sqrMagnitude > 0f)
                    {
                        moveDir = groundMoveDir.normalized;
                    }
                }

                _lastNonZeroMoveDirection = moveDir;
            }

            void Update()
            {
                if (!_inputBank || !_body)
                    return;

                if (_inputBank.moveVector.sqrMagnitude > 0f)
                {
                    _lastNonZeroMoveDirection = _inputBank.moveVector;
                }
                else
                {
                    _inputBank.moveVector = _lastNonZeroMoveDirection;
                }
            }
        }
    }
}
