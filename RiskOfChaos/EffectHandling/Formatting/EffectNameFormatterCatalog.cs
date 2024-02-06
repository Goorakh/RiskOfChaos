using HG;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public static class EffectNameFormatterCatalog
    {
        static Type[] _effectNameFormatterTypes = [];

        static readonly Dictionary<Type, int> _formatterTypeToIndexMap = [];

        [SystemInitializer]
        static void Init()
        {
            _effectNameFormatterTypes = Assembly.GetExecutingAssembly()
                                                .GetTypes()
                                                .Where(t => !t.IsAbstract && typeof(EffectNameFormatter).IsAssignableFrom(t) && t != typeof(EffectNameFormatter_None))
#if DEBUG
                                                .Where(t =>
                                                {
                                                    if (t.GetConstructor([]) is null)
                                                    {
                                                        Log.Error($"Formatter type {t.FullName} is missing parameterless constructor");
                                                        return false;
                                                    }

                                                    return true;
                                                })
#endif
                                                .Distinct()
                                                .OrderBy(t => t.FullName)
                                                .ToArray();

            for (int i = 0; i < _effectNameFormatterTypes.Length; i++)
            {
                _formatterTypeToIndexMap[_effectNameFormatterTypes[i]] = i;
            }
        }

        public static int GetFormatterTypeIndex(EffectNameFormatter formatter)
        {
            if (formatter is not null && _formatterTypeToIndexMap.TryGetValue(formatter.GetType(), out int formatterTypeIndex))
            {
                return formatterTypeIndex;
            }
            else
            {
                return -1;
            }
        }

        public static void Write(this NetworkWriter writer, EffectNameFormatter formatter)
        {
            int typeIndex = GetFormatterTypeIndex(formatter);
            writer.WritePackedIndex32(typeIndex);

            if (typeIndex != -1)
            {
                formatter.Serialize(writer);
            }
        }

        public static EffectNameFormatter ReadEffectNameFormatter(this NetworkReader reader)
        {
            int formatterTypeIndex = reader.ReadPackedIndex32();
            if (!ArrayUtils.IsInBounds(_effectNameFormatterTypes, formatterTypeIndex))
                return EffectNameFormatter_None.Instance;

            EffectNameFormatter formatter = (EffectNameFormatter)Activator.CreateInstance(_effectNameFormatterTypes[formatterTypeIndex]);
            formatter.Deserialize(reader);
            return formatter;
        }
    }
}
