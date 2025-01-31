using RoR2;
using RoR2.Skills;

namespace RiskOfChaos.Patches
{
    static class GenericSkillHooks
    {
        public delegate void OnGenericSkillAwakeGlobalDelegate(GenericSkill skill);
        public static event OnGenericSkillAwakeGlobalDelegate OnGenericSkillAwakeGlobal;

        public delegate void OnSkillChangedGlobalDelegate(GenericSkill skill, SkillDef previousSkill, SkillDef newSkill);
        public static event OnSkillChangedGlobalDelegate OnSkillChangedGlobal;
        public static event OnSkillChangedGlobalDelegate OnBaseSkillChangedGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GenericSkill.Awake += GenericSkill_Awake;
            On.RoR2.GenericSkill.SetSkillInternal += GenericSkill_SetSkillInternal;
            On.RoR2.GenericSkill.SetBaseSkill += GenericSkill_SetBaseSkill;
        }

        static void GenericSkill_Awake(On.RoR2.GenericSkill.orig_Awake orig, GenericSkill self)
        {
            orig(self);

            OnGenericSkillAwakeGlobal?.Invoke(self);
        }

        static void GenericSkill_SetSkillInternal(On.RoR2.GenericSkill.orig_SetSkillInternal orig, GenericSkill self, SkillDef newSkillDef)
        {
            SkillDef prevSkillDef = self.skillDef;

            orig(self, newSkillDef);

            if (prevSkillDef != self.skillDef)
            {
                OnSkillChangedGlobal?.Invoke(self, prevSkillDef, self.skillDef);
            }
        }

        static void GenericSkill_SetBaseSkill(On.RoR2.GenericSkill.orig_SetBaseSkill orig, GenericSkill self, SkillDef newSkillDef)
        {
            SkillDef prevBaseSkill = self.baseSkill;

            orig(self, newSkillDef);

            if (prevBaseSkill != self.baseSkill)
            {
                OnBaseSkillChangedGlobal?.Invoke(self, prevBaseSkill, self.baseSkill);
            }
        }
    }
}
