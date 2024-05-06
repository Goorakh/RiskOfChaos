using System;
using System.Reflection;

namespace RiskOfChaos.Utilities.Reflection
{
    public class FieldWrapper<TValue, TInstance>
    {
        readonly CachedFieldReference _field;

        public delegate TValue ConvertGetValueDelegate(object fieldValue, Type fieldType);
        public delegate object ConvertSetValueDelegate(TValue value, Type targetType);

        public ConvertGetValueDelegate ConvertGetValue = (_, _) => throw new NotImplementedException();
        public ConvertSetValueDelegate ConvertSetValue = (_, _) => throw new NotImplementedException();

        public FieldWrapper(CachedFieldReference fieldReference)
        {
            _field = fieldReference;
        }

        public TValue Get(TInstance instance)
        {
            FieldInfo fieldInfo = _field.FieldInfo.Value;
            if (fieldInfo == null)
                throw new MissingFieldException();

            if (fieldInfo.FieldType == typeof(TValue))
            {
                return (TValue)fieldInfo.GetValue(instance);
            }
            else
            {
                return ConvertGetValue(fieldInfo.GetValue(instance), fieldInfo.FieldType);
            }
        }

        public void Set(TInstance instance, TValue value)
        {
            FieldInfo fieldInfo = _field.FieldInfo.Value;
            if (fieldInfo == null)
                throw new MissingFieldException();

            if (fieldInfo.FieldType == typeof(TValue))
            {
                fieldInfo.SetValue(instance, value);
            }
            else
            {
                fieldInfo.SetValue(instance, ConvertSetValue(value, fieldInfo.FieldType));
            }
        }
    }
}
