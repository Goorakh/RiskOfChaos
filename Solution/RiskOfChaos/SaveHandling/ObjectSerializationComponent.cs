﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.SaveHandling
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(NetworkIdentity))]
    public class ObjectSerializationComponent : MonoBehaviour
    {
        static readonly List<ObjectSerializationComponent> _instances = [];
        public static readonly ReadOnlyCollection<ObjectSerializationComponent> Instances = new ReadOnlyCollection<ObjectSerializationComponent>(_instances);

        public bool IsSingleton;

        public bool IsDeserialized { get; private set; }

        NetworkIdentity _networkIdentity;

        readonly Dictionary<Type, ComponentSerializationInfo> _serializationInfoByComponentType = [];

        public NetworkHash128 AssetId => _networkIdentity.assetId;

        void Awake()
        {
            _networkIdentity = GetComponent<NetworkIdentity>();

            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                Type componentType = component.GetType();
                if (_serializationInfoByComponentType.ContainsKey(componentType))
                {
                    Log.Error($"Duplicate component types ({componentType.FullName}) for serialization on object {name}, this is not supported");
                    continue;
                }

                List<SerializableMemberInfo> serializableMembers = [];

                const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                foreach (MemberInfo member in componentType.GetAllMembersRecursive(FLAGS))
                {
                    SerializedMemberAttribute serializedMemberAttribute = member.GetCustomAttribute<SerializedMemberAttribute>();
                    if (serializedMemberAttribute == null)
                        continue;

                    switch (member)
                    {
                        case FieldInfo field:
                            if (field.IsLiteral)
                            {
                                Log.Error($"Cannot serialize constant field {componentType.FullName}.{field.Name}");
                                continue;
                            }

                            break;
                        case PropertyInfo property:
                            if (property.GetMethod == null)
                            {
                                Log.Error($"Cannot serialize property {componentType.FullName}.{property.Name}: Missing getter");
                                continue;
                            }

                            if (property.SetMethod == null)
                            {
                                Log.Error($"Cannot serialize property {componentType.FullName}.{property.Name}: Missing setter");
                                continue;
                            }

                            if (property.GetIndexParameters().Length > 0)
                            {
                                Log.Error($"Cannot serialize property {componentType.FullName}.{property.Name}: Indexed properties are not supported");
                                continue;
                            }

                            break;
                        default:
                            Log.Error($"Unsupported member type: {member.MemberType} ({componentType.FullName}.{member.Name})");
                            continue;
                    }

                    serializableMembers.Add(new SerializableMemberInfo(member, serializedMemberAttribute));
                }

                if (serializableMembers.Count > 0)
                {
                    _serializationInfoByComponentType.Add(componentType, new ComponentSerializationInfo(component, new ReadOnlyCollection<SerializableMemberInfo>(serializableMembers)));
                }
            }
        }

        void OnEnable()
        {
            _instances.Add(this);
        }

        void OnDisable()
        {
            _instances.Remove(this);
        }

        public void SerializeInto(SerializableGameObject serializedObject, JsonSerializer serializer)
        {
            serializedObject.PrefabAssetId = AssetId;

            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            List<SerializableObjectComponent> serializedComponents = new List<SerializableObjectComponent>(_serializationInfoByComponentType.Count);

            foreach (KeyValuePair<Type, ComponentSerializationInfo> kvp in _serializationInfoByComponentType)
            {
                Type componentType = kvp.Key;
                ComponentSerializationInfo serializationInfo = kvp.Value;

                List<SerializableObjectField> fields = new List<SerializableObjectField>(serializationInfo.SerializableMembers.Count);

                foreach (SerializableMemberInfo serializableMember in serializationInfo.SerializableMembers)
                {
                    JToken serializedValue;
                    try
                    {
                        object memberValue = serializableMember.GetValue(serializationInfo.ComponentInstance);
                        if (memberValue is null)
                        {
                            serializedValue = JValue.CreateNull();
                        }
                        else
                        {
                            serializedValue = JToken.FromObject(memberValue, serializer);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error_NoCallerPrefix($"Failed to serialize member {serializableMember.Name} on component {componentType.FullName}: {e}");
                        continue;
                    }

                    fields.Add(new SerializableObjectField
                    {
                        Name = serializableMember.Name,
                        Value = serializedValue
                    });
                }

                if (fields.Count > 0)
                {
                    serializedComponents.Add(new SerializableObjectComponent
                    {
                        ComponentType = componentType,
                        Fields = fields.ToArray()
                    });
                }
            }

            serializedObject.Components = serializedComponents.ToArray();
        }

        public void DeserializeFrom(SerializableGameObject serializedObject, JsonSerializer serializer)
        {
            IsDeserialized = true;

            foreach (SerializableObjectComponent serializedComponent in serializedObject.Components)
            {
                if (!_serializationInfoByComponentType.TryGetValue(serializedComponent.ComponentType, out ComponentSerializationInfo serializationInfo))
                {
                    Log.Warning($"Serializable object {name} is missing component found in serialized data: {serializedComponent.ComponentType}");
                    continue;
                }

                foreach (SerializableObjectField serializedField in serializedComponent.Fields)
                {
                    SerializableMemberInfo serializableMember = serializationInfo.SerializableMembers.FirstOrDefault(m => string.Equals(m.Name, serializedField.Name));
                    if (serializableMember == null)
                    {
                        Log.Warning($"Serializable object {name} component ({serializedComponent.ComponentType}) is missing field '{serializedField.Name}' found in serialized data");
                        continue;
                    }

                    object fieldValue;
                    try
                    {
                        fieldValue = serializedField.Value.ToObject(serializableMember.Type, serializer);
                    }
                    catch (Exception e)
                    {
                        Log.Error_NoCallerPrefix($"Serializable object {name} failed to deserialize field value ({serializedComponent.ComponentType}.{serializedField.Name}): {e}");
                        continue;
                    }

                    serializableMember.SetValue(serializationInfo.ComponentInstance, fieldValue);
                }
            }
        }

        record ComponentSerializationInfo(MonoBehaviour ComponentInstance, ReadOnlyCollection<SerializableMemberInfo> SerializableMembers);

        record SerializableMemberInfo(MemberInfo Member, SerializedMemberAttribute SerializationAttribute)
        {
            public string Name => SerializationAttribute.GetName(Member);

            public Type Type => Member switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => throw new NotImplementedException($"Member type {Member.MemberType} is not implemented")
            };

            public object GetValue(object instance)
            {
                switch (Member)
                {
                    case FieldInfo field:
                        return field.GetValue(instance);
                    case PropertyInfo property:
                        return property.GetValue(instance);
                    default:
                        throw new NotImplementedException($"Member type {Member.MemberType} is not implemented");
                }
            }

            public void SetValue(MonoBehaviour instance, object value)
            {
                switch (Member)
                {
                    case FieldInfo field:
                        field.SetValue(instance, value);
                        break;
                    case PropertyInfo property:
                        property.SetValue(instance, value);
                        break;
                    default:
                        throw new NotImplementedException($"Member type {Member.MemberType} is not implemented");
                }
            }
        }
    }
}