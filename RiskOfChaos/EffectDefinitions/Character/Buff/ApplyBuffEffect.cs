using HarmonyLib;
using HG;
using RiskOfChaos.Components;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    public abstract class ApplyBuffEffect : TimedEffect
    {
        static int[] _activeBuffStacks = Array.Empty<int>();

        [SystemInitializer(typeof(BuffCatalog))]
        static void Init()
        {
            _activeBuffStacks = BuffCatalog.GetPerBuffBuffer<int>();

            Run.onRunStartGlobal += _ =>
            {
                ArrayUtils.SetAll(_activeBuffStacks, 0);
            };
        }

        protected static int getBuffStackCount(BuffIndex buffIndex)
        {
            return ArrayUtils.GetSafe(_activeBuffStacks, (int)buffIndex);
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
            if (buff.isDebuff)
                return true;

            switch (buff.name)
            {
                // MysticsItems compat
                case "MysticsItems_Crystallized":
                case "MysticsItems_TimePieceSlow":

                case "bdBlinded":
                    return true;
            }

            return false;
        }

        protected static bool isCooldown(BuffDef buff)
        {
            if (buff.isCooldown)
                return true;

            switch (buff.name)
            {
                // LostInTransit compat
                case "RepulsionArmorCD":
                    return true;
            }

            return false;
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
            CharacterBody.readOnlyInstancesList.Do(addBuff);
            CharacterBody.onBodyStartGlobal += addBuff;

            if (ArrayUtils.IsInBounds(_activeBuffStacks, (int)_buffIndex))
            {
                _activeBuffStacks[(int)_buffIndex]++;
            }
            else
            {
                Log.Warning($"Buff index {_buffIndex} out of range (max={_activeBuffStacks.Length - 1})");
            }
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= addBuff;

            InstanceTracker.GetInstancesList<KeepBuff>().TryDo(keepBuff =>
            {
                if (keepBuff.BuffIndex == _buffIndex)
                {
                    keepBuff.BuffStackCount--;
                }
            });

            if (ArrayUtils.IsInBounds(_activeBuffStacks, (int)_buffIndex))
            {
                _activeBuffStacks[(int)_buffIndex]--;
            }
            else
            {
                Log.Warning($"Buff index {_buffIndex} out of range (max={_activeBuffStacks.Length - 1})");
            }
        }

        void addBuff(CharacterBody body)
        {
            try
            {
                KeepBuff.AddTo(body, _buffIndex);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to add buff {BuffCatalog.GetBuffDef(_buffIndex)} to {FormatUtils.GetBestBodyName(body)}: {ex}");
            }
        }
    }
}
