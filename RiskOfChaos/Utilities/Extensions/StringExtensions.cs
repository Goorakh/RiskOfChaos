using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class StringExtensions
    {
        static readonly StringBuilder _stringBuilder = new StringBuilder();

        public static string FilterChars(this string str, char[] invalidChars)
        {
            _stringBuilder.Clear();

            foreach (char c in str)
            {
                if (Array.IndexOf(invalidChars, c) == -1)
                {
                    _stringBuilder.Append(c);
                }
            }

            return _stringBuilder.ToString();
        }

        static readonly char[] _invalidConfigChars = (char[])AccessTools.DeclaredField(typeof(ConfigDefinition), "_invalidConfigChars")?.GetValue(null);

        public static string FilterConfigKey(this string key)
        {
            if (_invalidConfigChars == null)
            {
                Log.Error($"{nameof(_invalidConfigChars)} is null");
                return key;
            }

            return key.FilterChars(_invalidConfigChars);
        }
    }
}
