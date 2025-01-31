using EntityStates.Toolbot;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    public sealed class ToolbotSkillSwitchFix : MonoBehaviour
    {
        [SystemInitializer(typeof(BodyCatalog))]
        static void Init()
        {
            GameObject toolbotBodyPrefab = BodyCatalog.FindBodyPrefab("ToolbotBody");
            if (toolbotBodyPrefab)
            {
                toolbotBodyPrefab.AddComponent<ToolbotSkillSwitchFix>();
            }
            else
            {
                Log.Warning("Failed to find MUL-T body prefab");
            }
        }

        CharacterBody _body;

        EntityStateMachine _bodyStateMachine;
        EntityStateMachine _stanceStateMachine;

        GenericSkill _primary1Slot;
        GenericSkill _primary2Slot;

        void Awake()
        {
            _body = GetComponent<CharacterBody>();

            _bodyStateMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");
            _stanceStateMachine = EntityStateMachine.FindByCustomName(gameObject, "Stance");

            if (_body.skillLocator)
            {
                _primary1Slot = _body.skillLocator.FindSkillByFamilyName("ToolbotBodyPrimary1");
                _primary2Slot = _body.skillLocator.FindSkillByFamilyName("ToolbotBodyPrimary2");
            }
        }

        void OnEnable()
        {
            GenericSkillHooks.OnSkillChangedGlobal += onSkillChangedGlobal;
            GenericSkillHooks.OnBaseSkillChangedGlobal += onBaseSkillChangedGlobal;
        }

        void OnDisable()
        {
            GenericSkillHooks.OnSkillChangedGlobal -= onSkillChangedGlobal;
            GenericSkillHooks.OnBaseSkillChangedGlobal -= onBaseSkillChangedGlobal;
        }

        void onSkillChangedGlobal(GenericSkill skill, SkillDef previousSkill, SkillDef newSkill)
        {
            if (!skill)
                return;

            if (skill == _primary1Slot || skill == _primary2Slot)
            {
                fixDualWieldSkillOverrides(skill, previousSkill, newSkill);
            }
        }

        void onBaseSkillChangedGlobal(GenericSkill skill, SkillDef previousSkill, SkillDef newSkill)
        {
            if (!skill)
                return;

            if (skill == _body.skillLocator.primary)
            {
                onPrimaryBaseSkillChanged(skill, previousSkill, newSkill);
            }
        }

        void fixDualWieldSkillOverrides(GenericSkill changedSkill, SkillDef prev, SkillDef current)
        {
            GenericSkill secondarySkill = _body.skillLocator.secondary;

            bool changedAnySecondarySkillOverride = false;

            for (int i = 0; i < secondarySkill.skillOverrides.Length; i++)
            {
                GenericSkill.SkillOverride skillOverride = secondarySkill.skillOverrides[i];
                if (skillOverride == null)
                    continue;

                if (skillOverride.source is ToolbotDualWieldBase toolbotDualWieldState &&
                    changedSkill == toolbotDualWieldState.primary2Slot &&
                    skillOverride.skillDef == prev)
                {
                    skillOverride.skillDef = current;
                    changedAnySecondarySkillOverride = true;
                }
            }

            if (changedAnySecondarySkillOverride)
            {
                secondarySkill.PickCurrentOverride();
            }
        }

        void onPrimaryBaseSkillChanged(GenericSkill primary, SkillDef prev, SkillDef current)
        {
            if (_stanceStateMachine.state is ToolbotStanceBase stanceState)
            {
                ToolbotWeaponSkillDef newToolbotSkillDef = current as ToolbotWeaponSkillDef;

                if (newToolbotSkillDef)
                {
                    stanceState.SendWeaponStanceToAnimator(newToolbotSkillDef);

                    stanceState.PlayAnimation("Stance, Additive", newToolbotSkillDef.entryAnimState);
                }

                stanceState.UpdateCrosshairParameters(newToolbotSkillDef);
            }
        }
    }
}
