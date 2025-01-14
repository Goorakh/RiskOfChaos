﻿using RiskOfChaos.Utilities.Extensions;
using RoR2.Skills;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectUtils.Character.AllSkillsAgile
{
    public static class OverrideSkillsAgile
    {
        readonly struct SkillDefIsAgileOverride
        {
            public readonly SkillDef SkillDef;

            public readonly bool OriginalCancelSprintingOnActivation;
            public readonly bool OriginalCanceledFromSprinting;

            public SkillDefIsAgileOverride(SkillDef skillDef)
            {
                SkillDef = skillDef;

                OriginalCancelSprintingOnActivation = SkillDef.cancelSprintingOnActivation;
                SkillDef.cancelSprintingOnActivation = false;

                OriginalCanceledFromSprinting = SkillDef.canceledFromSprinting;
                SkillDef.canceledFromSprinting = false;
            }

            public readonly void Undo()
            {
                if (!SkillDef)
                    return;

                SkillDef.cancelSprintingOnActivation = OriginalCancelSprintingOnActivation;
                SkillDef.canceledFromSprinting = OriginalCanceledFromSprinting;
            }
        }

        static readonly List<SkillDefIsAgileOverride> _skillIsAgileOverrides = [];

        static int _allSkillsAgileCount = 0;
        public static int AllSkillsAgileCount
        {
            get => _allSkillsAgileCount;
            set
            {
                if (_allSkillsAgileCount == value)
                    return;

                bool wasActive = _allSkillsAgileCount > 0;
                bool isActive = value > 0;

                _allSkillsAgileCount = value;

                if (wasActive != isActive)
                {
                    if (isActive)
                    {
                        _skillIsAgileOverrides.EnsureCapacity(SkillCatalog.allSkillDefs.Count());

                        SkillCatalog.allSkillDefs.TryDo(skillDef =>
                        {
                            if (skillDef.cancelSprintingOnActivation || skillDef.canceledFromSprinting)
                            {
                                _skillIsAgileOverrides.Add(new SkillDefIsAgileOverride(skillDef));
                            }
                        });

                        Log.Debug("Skills agile override enabled");
                    }
                    else
                    {
                        _skillIsAgileOverrides.TryDo(isAgileOverride => isAgileOverride.Undo());
                        _skillIsAgileOverrides.Clear();

                        Log.Debug("Skills agile override cleared");
                    }
                }
            }
        }

        public static bool IsAllSkillsAgile => AllSkillsAgileCount > 0;
    }
}
