using HG;
using RoR2;
using System;
using System.Collections.Generic;
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
            List<Type> effectNameFormatterTypes = [];

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsAbstract || !typeof(EffectNameFormatter).IsAssignableFrom(type))
                    continue;

                ConstructorInfo parameterlessConstructor = type.GetConstructor([]);
                if (parameterlessConstructor == null)
                {
                    Log.Error($"Formatter type {type.FullName} is missing parameterless constructor");
                    continue;
                }

                effectNameFormatterTypes.Add(type);
            }

            _effectNameFormatterTypes = effectNameFormatterTypes.ToArray();
            Array.Sort(_effectNameFormatterTypes, (a, b) => a.FullName.CompareTo(b.FullName));

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

            Type formatterType = ArrayUtils.GetSafe(_effectNameFormatterTypes, formatterTypeIndex);
            if (formatterType == null)
                return null;

            EffectNameFormatter formatter = (EffectNameFormatter)Activator.CreateInstance(formatterType);
            formatter.Deserialize(reader);
            return formatter;
        }
    }
}
