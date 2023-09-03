using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class GenericSkillTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GenericSkill.Awake += (orig, self) =>
            {
                orig(self);

                GenericSkillTracker tracker = self.gameObject.AddComponent<GenericSkillTracker>();
                tracker.Skill = self;
            };
        }

        public GenericSkill Skill { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        void Update()
        {
            if (!Skill)
            {
                Destroy(this);
            }
        }
    }
}
