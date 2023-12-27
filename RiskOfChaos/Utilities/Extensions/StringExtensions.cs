using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Text;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class StringExtensions
    {
        public static string FilterChars(this string str, char[] invalidChars)
        {
            if (str.IndexOfAny(invalidChars) == -1)
                return str;

            StringBuilder sb = HG.StringBuilderPool.RentStringBuilder();

            foreach (char c in str)
            {
                if (Array.IndexOf(invalidChars, c) == -1)
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString();
            HG.StringBuilderPool.ReturnStringBuilder(sb);
            return result;
        }

        static readonly char[] _invalidConfigDefinitionChars = (char[])AccessTools.DeclaredField(typeof(ConfigDefinition), "_invalidConfigChars")?.GetValue(null);

        public static string FilterConfigKey(this string key)
        {
            if (_invalidConfigDefinitionChars == null)
            {
                Log.Error($"{nameof(_invalidConfigDefinitionChars)} is null");
                return key;
            }

            return key.FilterChars(_invalidConfigDefinitionChars);
        }
    }
}
