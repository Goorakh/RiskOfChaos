using HG;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    public abstract class ApplyBuffEffect : TimedEffect
    {
        static int[] _appliedBuffStacks = Array.Empty<int>();

        [SystemInitializer(typeof(BuffCatalog))]
        static void Init()
        {
            _appliedBuffStacks = BuffCatalog.GetPerBuffBuffer<int>();
        }

        protected static int getBuffStackCount(BuffIndex buffIndex)
        {
            return ArrayUtils.GetSafe(_appliedBuffStacks, (int)buffIndex);
        }

        protected static bool canSelectBuff(BuffIndex buffIndex)
        {
            if (buffIndex == BuffIndex.None)
                return false;

            BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
            if (!buffDef)
                return false;

            return buffDef.canStack || getBuffStackCount(buffIndex) == 0;
        }

        protected static IEnumerable<BuffIndex> filterSelectableBuffs(IEnumerable<BuffIndex> buffIndices)
        {
            return buffIndices.Where(canSelectBuff);
        }

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

            if (ArrayUtils.IsInBounds(_appliedBuffStacks, (int)_buffIndex))
            {
                _appliedBuffStacks[(int)_buffIndex]++;
            }
            else
            {
                Log.Warning($"Buff index {_buffIndex} out of range (max={_appliedBuffStacks.Length - 1})");
            }
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= addBuff;

            foreach (KeepBuff keepBuff in InstanceTracker.GetInstancesList<KeepBuff>())
            {
                if (keepBuff.BuffIndex == _buffIndex)
                {
                    keepBuff.BuffStackCount--;
                }
            }

            if (ArrayUtils.IsInBounds(_appliedBuffStacks, (int)_buffIndex))
            {
                _appliedBuffStacks[(int)_buffIndex]--;
            }
            else
            {
                Log.Warning($"Buff index {_buffIndex} out of range (max={_appliedBuffStacks.Length - 1})");
            }
        }

        void addBuff(CharacterBody body)
        {
            KeepBuff.AddTo(body, _buffIndex);
        }
    }
}
