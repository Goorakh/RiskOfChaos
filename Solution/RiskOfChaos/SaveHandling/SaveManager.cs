using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RiskOfChaos.Content;
using RiskOfChaos.ModCompatibility;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.Serialization.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.SaveHandling
{
    static class SaveManager
    {
        [Obsolete]
        public delegate void CollectSaveDataDelegate(ref SaveContainer container);
        [Obsolete]
        public static event CollectSaveDataDelegate CollectSaveData;

        [Obsolete]
        public delegate void OnSaveDataLoadedDelegate(in SaveContainer container);
        [Obsolete]
        public static event OnSaveDataLoadedDelegate LoadSaveData
        {
            add
            {
            }
            remove
            {
            }
        }

        [Obsolete]
        public static bool UseSaveData => ProperSaveCompat.Active;

        public static JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                Converters = [
                    new XoroshiroRngConverter(),
                    new NetworkHash128Converter(),
                    new BuffIndexConverter(),
                    new EquipmentIndexConverter(),
                    new ChaosEffectIndexConverter(),
                    new StringEnumConverter(new DefaultNamingStrategy(), false),
                ],
                Error = onSerializerError,
                TraceWriter = new SerializationTraceWriter()
            };
        }

        internal static string CollectAllSaveData()
        {
            JsonSerializerSettings serializerSettings = GetSerializerSettings();

            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(serializerSettings);

            SaveDataContainer container = new SaveDataContainer();

            List<SerializableGameObject> serializedObjects = new List<SerializableGameObject>(ObjectSerializationComponent.Instances.Count);
            foreach (ObjectSerializationComponent serializationComponent in ObjectSerializationComponent.Instances)
            {
                SerializableGameObject serializedObject = new SerializableGameObject();

                try
                {
                    serializationComponent.SerializeInto(serializedObject, jsonSerializer);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to serialize object {serializationComponent.name}: {e}");
                    continue;
                }

                serializedObjects.Add(serializedObject);
            }

            container.Objects = serializedObjects.ToArray();

            return JsonConvert.SerializeObject(container, serializerSettings);
        }

        internal static void OnSaveDataLoaded(string saveDataJson)
        {
            JsonSerializerSettings serializerSettings = GetSerializerSettings();

            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(serializerSettings);

            SaveDataContainer container = JsonConvert.DeserializeObject<SaveDataContainer>(saveDataJson, serializerSettings);

            if (container.Objects != null)
            {
                Dictionary<NetworkHash128, ObjectSerializationComponent> existingSingletonInstances = [];

                foreach (ObjectSerializationComponent serializationComponent in ObjectSerializationComponent.Instances)
                {
                    if (serializationComponent.IsSingleton)
                    {
                        if (existingSingletonInstances.TryAdd(serializationComponent.AssetId, serializationComponent))
                        {
#if DEBUG
                            Log.Debug($"Found serializable singleton object: {serializationComponent.name}");
#endif
                        }
                        else
                        {
                            Log.Warning($"Multiple instances found of serializable object {serializationComponent.name} (id={serializationComponent.AssetId})");
                        }
                    }
                }

                foreach (SerializableGameObject serializedObject in container.Objects)
                {
                    if (!existingSingletonInstances.TryGetValue(serializedObject.PrefabAssetId, out ObjectSerializationComponent serializationComponent))
                    {
                        if (!RoCContent.NetworkedPrefabs.PrefabsByAssetId.TryGetValue(serializedObject.PrefabAssetId, out GameObject prefab))
                        {
                            Log.Error($"Failed to find network prefab for asset id {serializedObject.PrefabAssetId}");
                            continue;
                        }

                        GameObject gameObject = GameObject.Instantiate(prefab);
                        NetworkServer.Spawn(gameObject);

                        serializationComponent = gameObject.GetComponent<ObjectSerializationComponent>();
                        if (!serializationComponent)
                        {
                            Log.Error($"Serializable object {gameObject} is missing serialization component");
                            continue;
                        }
                    }

                    try
                    {
                        serializationComponent.DeserializeFrom(serializedObject, jsonSerializer);
                    }
                    catch (Exception e)
                    {
                        Log.Error_NoCallerPrefix($"Failed to deserialize object {serializationComponent.name}: {e}"); ;
                    }
                }
            }
        }

        static void onSerializerError(object sender, ErrorEventArgs eventArgs)
        {
            bool handled = false;
            if (eventArgs.ErrorContext.Handled)
            {
#if DEBUG
                handled = true;
#else
                return;
#endif
            }

            string log = $"Error deserializing json (at {eventArgs.ErrorContext.Path}): {eventArgs.ErrorContext.Error}";
#if DEBUG
            if (handled)
            {
                Log.Info($"[Handled] {log}");
            }
            else
#endif
            {
                Log.Error(log);
            }
        }

        class SerializationTraceWriter : ITraceWriter
        {
            readonly StringBuilder _stringBuilder = new StringBuilder();
            readonly object _stringBuilderLock = new object();

            const TraceLevel LEVEL_FILTER =
#if DEBUG
                TraceLevel.Verbose;
#else
                TraceLevel.Warning;
#endif

            public TraceLevel LevelFilter { get; } = LEVEL_FILTER;

            public void Trace(TraceLevel level, string message, Exception ex)
            {
                lock (_stringBuilderLock)
                {
                    _stringBuilder.Clear();
                    _stringBuilder.Append(message);

                    if (ex != null)
                    {
                        _stringBuilder.Append(": ");
                        _stringBuilder.Append(ex);
                    }

                    switch (level)
                    {
                        case TraceLevel.Error:
                            Log.Error(_stringBuilder);
                            break;
                        case TraceLevel.Warning:
                            Log.Warning(_stringBuilder);
                            break;
                        case TraceLevel.Info:
                            Log.Info(_stringBuilder);
                            break;
#if DEBUG
                        case TraceLevel.Verbose:
                            Log.Debug(_stringBuilder);
                            break;
#endif
                    }
                }
            }
        }
    }
}
