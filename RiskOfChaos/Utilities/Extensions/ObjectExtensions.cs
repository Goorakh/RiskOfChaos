using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ObjectExtensions
    {
        public static T ShallowCopy<T>(this T source, BindingFlags fieldBindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (source is null)
                return default;

            if (source is ValueType)
                return source;

            if (source is ICloneable cloneable)
                return (T)cloneable.Clone();

            T copyInstance = (T)Activator.CreateInstance(source.GetType());
            source.ShallowCopy(ref copyInstance, fieldBindingFlags);
            return copyInstance;
        }

        public static void ShallowCopy<T>(this T source, ref T dest, BindingFlags fieldBindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (source is null)
            {
                dest = default;
                return;
            }

            if (source is ValueType)
            {
                dest = source;
                return;
            }

            if (source is ICloneable cloneable)
            {
                dest = (T)cloneable.Clone();
                return;
            }

            foreach (FieldInfo field in source.GetType().GetFields(fieldBindingFlags))
            {
                try
                {
                    object fieldValue = field.GetValue(source);

                    Type fieldType = field.FieldType;
                    if (fieldType.IsClass)
                    {
                        if (typeof(ICloneable).IsAssignableFrom(fieldType))
                        {
                            fieldValue = ((ICloneable)fieldValue).Clone();
                        }
                        else if (fieldType.IsGenericType)
                        {
                            if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                if (fieldValue is not null)
                                {
                                    try
                                    {
                                        // copyInstance.listField = new List<T>(source.listField)
                                        fieldValue = Activator.CreateInstance(fieldType, fieldValue);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning_NoCallerPrefix($"Failed to copy list type {fieldType.FullDescription()} for {field.DeclaringType.FullDescription()}.{field.Name}, same instance of list object will be used instead: {ex}");
                                    }
                                }
                            }
                        }
                    }

                    field.SetValue(dest, fieldValue);
                }
                catch (Exception ex)
                {
                    Log.Warning_NoCallerPrefix($"Failed to set copy field value {field.DeclaringType.FullName}.{field.Name} ({field.FieldType.FullName}): {ex}");
                }
            }
        }
    }
}
