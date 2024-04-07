using HarmonyLib;
using HG;
using RiskOfChaos.Collections.CatalogIndex;
using RiskOfChaos.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    public abstract class ApplyBuffEffect : TimedEffect
    {
        static uint[] _activeBuffStacks = [];

        [SystemInitializer(typeof(BuffCatalog))]
        static void Init()
        {
            _activeBuffStacks = BuffCatalog.GetPerBuffBuffer<uint>();

            Run.onRunStartGlobal += _ =>
            {
                ArrayUtils.SetAll(_activeBuffStacks, 0U);
            };
        }

        protected static uint getBuffStackCount(BuffIndex buffIndex)
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

            uint stackCount = getBuffStackCount(buffIndex);
            return stackCount < int.MaxValue && (buffDef.canStack || stackCount == 0);
        }

        protected static IEnumerable<BuffIndex> filterSelectableBuffs(IEnumerable<BuffIndex> buffIndices)
        {
            return buffIndices.Where(canSelectBuff);
        }

        static readonly BuffIndexCollection _isDebuffOverrideList = new BuffIndexCollection([
            // MysticsItems compat
            "MysticsItems_Crystallized",
            "MysticsItems_TimePieceSlow",

            // Starstorm2 compat
            "bdMULENet",

            "bdBlinded",
        ]);

        protected static bool isDebuff(BuffDef buff)
        {
            if (buff.isDebuff)
                return true;

            if (_isDebuffOverrideList.Contains(buff.buffIndex))
                return true;

            return false;
        }

        static readonly BuffIndexCollection _isCooldownOverrideList = new BuffIndexCollection([
            // LostInTransit compat
            "RepulsionArmorCD",

            // Starstorm2 compat
            "BuffTerminationCooldown",
        ]);

        protected static bool isCooldown(BuffDef buff)
        {
            if (buff.isCooldown)
                return true;

            if (_isCooldownOverrideList.Contains(buff.buffIndex))
                return true;

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

        protected abstract BuffIndex getBuffIndexToApply();

        protected BuffIndex _buffIndex = BuffIndex.None;

        protected uint _buffCount = 1;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _buffIndex = getBuffIndexToApply();
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.WritePackedIndex32((int)_buffIndex);
            writer.WritePackedUInt32(_buffCount);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            _buffIndex = (BuffIndex)reader.ReadPackedIndex32();
            _buffCount = reader.ReadPackedUInt32();
        }

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.Do(addBuff);
            CharacterBody.onBodyStartGlobal += addBuff;

            if (ArrayUtils.IsInBounds(_activeBuffStacks, (int)_buffIndex))
            {
                _activeBuffStacks[(int)_buffIndex] += _buffCount;
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
                    keepBuff.BuffStackCount -= _buffCount;
                }
            });

            if (ArrayUtils.IsInBounds(_activeBuffStacks, (int)_buffIndex))
            {
                _activeBuffStacks[(int)_buffIndex] -= _buffCount;
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
                KeepBuff.AddTo(body, _buffIndex, _buffCount);
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Failed to add buff {BuffCatalog.GetBuffDef(_buffIndex)} to {FormatUtils.GetBestBodyName(body)}: {ex}");
                return;
            }

            onBuffApplied(body);
        }

        protected virtual void onBuffApplied(CharacterBody body)
        {
        }
    }
}
