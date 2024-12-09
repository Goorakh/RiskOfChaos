using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RiskOfChaos.Utilities.Reflection
{
    public class CachedFieldReference
    {
        readonly Type _declaringType;

        readonly string _fieldName;
        readonly Regex _fieldNameRegex;
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

        public CachedFieldReference(Type declaringType, Type fieldType, BindingFlags bindingFlags) : this(declaringType, string.Empty, fieldType, bindingFlags)
        {
            if (fieldType is null)
                throw new ArgumentNullException(nameof(fieldType));
        }

        public CachedFieldReference(Type declaringType, Regex fieldNameRegex, Type fieldType, BindingFlags bindingFlags) : this(declaringType, string.Empty, fieldType, bindingFlags)
        {
            if (fieldNameRegex is null)
                throw new ArgumentNullException(nameof(fieldNameRegex));

            _fieldNameRegex = fieldNameRegex;
        }

        public CachedFieldReference(Type declaringType, Regex fieldNameRegex, BindingFlags bindingFlags) : this(declaringType, fieldNameRegex, null, bindingFlags)
        {
        }

        FieldInfo getFieldInfo()
        {
            FieldInfo[] fields = _declaringType.GetFields(_bindingFlags);
            List<FieldInfo> matchingFields = new List<FieldInfo>(fields.Length);

            foreach (FieldInfo field in fields)
            {
                if (!string.IsNullOrWhiteSpace(_fieldName) && !string.Equals(field.Name, _fieldName))
                    continue;

                if (_fieldNameRegex != null && !_fieldNameRegex.IsMatch(field.Name))
                    continue;

                if (_fieldType != null && field.FieldType != _fieldType)
                    continue;

                matchingFields.Add(field);
            }

            if (matchingFields.Count > 1)
                throw new AmbiguousMatchException();

            if (matchingFields.Count < 1)
            {
                Log.Debug($"Could not find field: declaring type={_declaringType.FullName}, type={_fieldType?.FullName}, name={_fieldName}, name regex={_fieldNameRegex}");
                return null;
            }

            return matchingFields[0];
        }
    }
}
