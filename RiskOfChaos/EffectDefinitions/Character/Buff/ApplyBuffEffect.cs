using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    public abstract class ApplyBuffEffect : TimedEffect
    {
        protected static bool isDebuff(BuffDef buff)
        {
            return buff.isDebuff || buff.name == "bdBlinded";
        }

        protected static bool isDOT(BuffDef buff)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            DotController.DotDef[] dotDefs = DotController.dotDefs;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            foreach (DotController.DotDef dotDef in dotDefs)
            {
                if (dotDef == null)
                    continue;

                if (dotDef.associatedBuff == buff)
                {
                    return true;
                }
            }

            return false;
        }

        protected BuffIndex _buffIndex;

        protected abstract BuffIndex getBuffIndexToApply();

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _buffIndex = getBuffIndexToApply();
        }

        public override void OnStart()
        {
            CharacterBody.onBodyStartGlobal += addBuff;

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                addBuff(body);
            }
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= addBuff;
        }

        void addBuff(CharacterBody body)
        {
            KeepBuff.AddTo(body, _buffIndex);
        }
    }
}
