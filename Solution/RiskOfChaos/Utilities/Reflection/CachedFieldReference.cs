using System;
using System.Collections.Generic;
using System.Reflection;

namespace RiskOfChaos.Utilities.Reflection
{
    public class CachedFieldReference
    {
        readonly Type _declaringType;

        readonly string _fieldName;
        readonly Type _fieldType;

        readonly BindingFlags _bindingFlags;

        public readonly Lazy<FieldInfo> FieldInfo;

        public CachedFieldReference(Type declaringType, string name, Type fieldType, BindingFlags bindingFlags)
        {
            _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            _fieldName = name;
            _fieldType = fieldType;
            _bindingFlags = bindingFlags;

            FieldInfo = new Lazy<FieldInfo>(getFieldInfo);
        }

        public CachedFieldReference(Type declaringType, string name, BindingFlags bindingFlags) : this(declaringType, name, null, bindingFlags)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
        }

        public CachedFieldReference(Type declaringType, Type fieldType, BindingFlags bindingFlags) : this(declaringType, null, fieldType, bindingFlags)
        {
            if (fieldType is null)
                throw new ArgumentNullException(nameof(fieldType));
        }

        FieldInfo getFieldInfo()
        {
            if (_fieldName != null)
            {
                FieldInfo fieldInfo = _declaringType.GetField(_fieldName, _bindingFlags);
                if (fieldInfo != null && (_fieldType == null || fieldInfo.FieldType == _fieldType))
                {
                    return fieldInfo;
                }
            }

            if (_fieldType != null)
            {
                FieldInfo[] fields = _declaringType.GetFields(_bindingFlags);

                List<FieldInfo> matchingFields = new List<FieldInfo>(fields.Length);
                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType == _fieldType)
                    {
                        matchingFields.Add(field);
                    }
                }

                return matchingFields.Count switch
                {
                    0 => null,
                    1 => matchingFields[0],
                    _ => throw new AmbiguousMatchException()
                };
            }

            Log.Info($"Could not find field: declaring type={_declaringType.FullName}, type={_fieldType?.FullName}, name={_fieldName}");
            return null;
        }
    }
}
