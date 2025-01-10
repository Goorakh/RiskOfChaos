using HG;
using RoR2;
using System;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class BuffUtils
    {
        static string[] _buffNameTokenLookup = [];

        [SystemInitializer(typeof(BuffCatalog))]
        static void Init()
        {
            _buffNameTokenLookup = new string[BuffCatalog.buffCount];
            Array.Fill(_buffNameTokenLookup, string.Empty);

            StringBuilder buffNameBuilder = HG.StringBuilderPool.RentStringBuilder();

            for (int i = 0; i < BuffCatalog.buffCount; i++)
            {
                BuffIndex buffIndex = (BuffIndex)i;

                BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
                if (!buffDef)
                    continue;

                string buffName = buffDef.name;

                int nameStartIndex = 0;
                if (buffName.StartsWith("bd"))
                {
                    nameStartIndex += 2;
                }

                buffNameBuilder.Clear();

                buffNameBuilder.EnsureCapacity(buffName.Length + 15);

                buffNameBuilder.Append("ROC_BUFF_");

                for (int j = nameStartIndex; j < buffName.Length; j++)
                {
                    char c = buffName[j];
                    if (j > nameStartIndex && char.IsUpper(c))
                    {
                        buffNameBuilder.Append('_');
                    }

                    buffNameBuilder.Append(char.ToUpper(c));
                }

                buffNameBuilder.Append("_NAME");

                _buffNameTokenLookup[i] = buffNameBuilder.ToString();
            }

            buffNameBuilder = HG.StringBuilderPool.ReturnStringBuilder(buffNameBuilder);
        }

        public static string GetBuffNameToken(BuffIndex buffIndex)
        {
            return ArrayUtils.GetSafe(_buffNameTokenLookup, (int)buffIndex, string.Empty);
        }

        public static string GetBuffNameToken(BuffDef buffDef)
        {
            return GetBuffNameToken(buffDef ? buffDef.buffIndex : BuffIndex.None);
        }

        public static string GetLocalizedBuffName(BuffDef buffDef)
        {
            if (!buffDef)
                return string.Empty;

            if (buffDef.isElite)
            {
                string eliteModifierToken = buffDef.eliteDef.modifierToken;
                if (!string.IsNullOrEmpty(eliteModifierToken) && !Language.IsTokenInvalid(eliteModifierToken))
                {
                    return Language.GetStringFormatted(eliteModifierToken, string.Empty).Trim();
                }
            }

            string nameToken = GetBuffNameToken(buffDef);
            if (!string.IsNullOrEmpty(nameToken) && !Language.IsTokenInvalid(nameToken))
            {
                return Language.GetString(nameToken);
            }

            return string.Empty;
        }

        [ConCommand(commandName = "roc_print_unnamed_buffs", flags = ConVarFlags.None, helpText = "Prints all unnamed buffs to the console.")]
        static void CCPrintUnnamedBuffs(ConCommandArgs args)
        {
            bool ignoreHidden = args.GetArgBool(0);

            for (int i = 0; i < BuffCatalog.buffCount; i++)
            {
                BuffIndex buffIndex = (BuffIndex)i;
                BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
                if (!buffDef || (ignoreHidden && buffDef.isHidden))
                    continue;

                string nameToken = GetBuffNameToken(buffIndex);

                if (string.IsNullOrEmpty(nameToken) || Language.IsTokenInvalid(nameToken))
                {
                    Debug.Log($"{buffDef.name}: {nameToken}");
                }
            }
        }
    }
}
