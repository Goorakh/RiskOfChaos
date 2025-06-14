using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2.ExpansionManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities
{
    public sealed class SpawnPool<T> : IReadOnlyCollection<T> where T : UnityEngine.Object
    {
        static readonly WeightedSelection<T> _sharedSpawnSelection = new WeightedSelection<T>();

        readonly List<Entry> _entries = [];

#if DEBUG
        public bool DebugTest;
        int _debugTestIndex = 0;
#endif

        public delegate IReadOnlyList<ExpansionDef> RequiredExpansionsProviderDelegate(T entry);

        RequiredExpansionsProviderDelegate _requiredExpansionsProvider = entry => [];
        public RequiredExpansionsProviderDelegate RequiredExpansionsProvider
        {
            get
            {
                return _requiredExpansionsProvider;
            }
            set
            {
                _requiredExpansionsProvider = value;
                refreshAllRequiredExpansions();
            }
        }

        public delegate bool IsEntryAvailableDelegate(T entry);
        public event IsEntryAvailableDelegate CalcIsEntryAvailable;

        public bool AnyAvailable
        {
            get
            {
                foreach (Entry entry in _entries)
                {
                    if (IsAvailable(entry))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int Count => _entries.Count;

        public int Capacity
        {
            get => _entries.Capacity;
            set => _entries.Capacity = value;
        }

        void generateSpawnSelection(WeightedSelection<T> weightedSelection)
        {
            weightedSelection.Clear();
            weightedSelection.EnsureCapacity(_entries.Count);

            foreach (Entry entry in _entries)
            {
                if (IsAvailable(entry))
                {
                    weightedSelection.AddChoice(entry.Asset, entry.Weight);
                }
            }
        }

        public bool IsAvailable(Entry entry)
        {
            if (!entry.IsValid)
                return false;

            if (!ExpansionUtils.AllExpansionsEnabled(entry.RequiredExpansions))
                return false;

            bool customIsAvailable = true;
            if (CalcIsEntryAvailable != null)
            {
                foreach (IsEntryAvailableDelegate isAvailableDelegate in CalcIsEntryAvailable.GetInvocationList().Cast<IsEntryAvailableDelegate>())
                {
                    if (!isAvailableDelegate(entry.Asset))
                    {
                        customIsAvailable = false;
                        break;
                    }
                }
            }

            if (!customIsAvailable)
                return false;

            return true;
        }

        public T PickRandomEntry(Xoroshiro128Plus rng)
        {
#if DEBUG
            if (DebugTest)
            {
                Entry entry;
                do
                {
                    entry = _entries[_debugTestIndex++ % _entries.Count];
                } while (!IsAvailable(entry));

                return entry.Asset;
            }
#endif

            generateSpawnSelection(_sharedSpawnSelection);
            return _sharedSpawnSelection.Evaluate(rng.nextNormalizedFloat);
        }

        public WeightedSelection<T> GetSpawnSelection()
        {
            WeightedSelection<T> spawnSelection = new WeightedSelection<T>();
            generateSpawnSelection(spawnSelection);
            return spawnSelection;
        }

        public void EnsureCapacity(int capacity)
        {
            if (Capacity < capacity)
                Capacity = capacity;
        }

        public void TrimExcess()
        {
            _entries.TrimExcess();
        }

        void refreshAllRequiredExpansions()
        {
            foreach (Entry entry in _entries)
            {
                if (entry.IsFullyLoaded)
                {
                    refreshRequiredExpansions(entry);
                }
            }
        }

        void refreshRequiredExpansions(Entry entry)
        {
            entry.SetRequiredExpansions(_requiredExpansionsProvider(entry.Asset));
        }

#if DEBUG
        public void DebugPrintEntries()
        {
            List<Entry> entries = new List<Entry>(_entries);
            entries.Sort((a, b) =>
            {
                ReadOnlyArray<ExpansionDef> expansionsA = a.RequiredExpansions;
                ReadOnlyArray<ExpansionDef> expansionsB = b.RequiredExpansions;

                if (expansionsA.Length != expansionsB.Length)
                {
                    return expansionsB.Length - expansionsA.Length;
                }

                for (int i = 0; i < expansionsA.Length; i++)
                {
                    int compare = string.Compare(expansionsA[i].name, expansionsB[i].name, true);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }

                return 0;
            });

            foreach (Entry entry in entries)
            {
                Log.Debug_NoCallerPrefix(entry);
            }
        }
#endif

        public void AddEntry(Entry entry)
        {
            _entries.Add(entry);

            entry.OnEntryLoaded += refreshRequiredExpansions;
        }

        public void AddEntry(T asset, SpawnPoolEntryParameters parameters)
        {
            AddEntry(new Entry(asset, parameters));
        }

        public Entry LoadEntry(string assetGuid, SpawnPoolEntryParameters parameters)
        {
            return Entry.LoadAsync(assetGuid, parameters);
        }

        public void AddAssetEntry(string assetGuid, SpawnPoolEntryParameters parameters)
        {
            AddEntry(LoadEntry(assetGuid, parameters));
        }

        public Entry LoadEntry<TAsset>(string assetGuid, SpawnPoolEntryParameters parameters, Converter<TAsset, T> assetConverter) where TAsset : UnityEngine.Object
        {
            return Entry.LoadAsync(assetGuid, parameters, assetConverter);
        }

        public void AddAssetEntry<TAsset>(string assetGuid, SpawnPoolEntryParameters parameters, Converter<TAsset, T> assetConverter) where TAsset : UnityEngine.Object
        {
            AddEntry(LoadEntry(assetGuid, parameters, assetConverter));
        }

        public Entry LoadEntry(string assetGuid, SpawnPoolEntryParameters parameters, Converter<T, T> assetConverter)
        {
            return LoadEntry<T>(assetGuid, parameters, assetConverter);
        }

        public void AddAssetEntry(string assetGuid, SpawnPoolEntryParameters parameters, Converter<T, T> assetConverter)
        {
            AddAssetEntry<T>(assetGuid, parameters, assetConverter);
        }

        public Entry[] GroupEntries(Entry[] entries, float weightMultiplier = 1f)
        {
            foreach (Entry entry in entries)
            {
                entry.Weight *= weightMultiplier / entries.Length;
            }

            return entries;
        }

        public void AddGroupedEntries(Entry[] entries, float weightMultiplier = 1f)
        {
            EnsureCapacity(Count + entries.Length);

            foreach (Entry entry in GroupEntries(entries, weightMultiplier))
            {
                AddEntry(entry);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _entries.Select(e => e.Asset).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public sealed class Entry
        {
            public delegate void OnEntryLoadedDelegate(Entry entry);

            public T Asset { get; private set; }

            public float Weight { get; set; }

            public bool IsValid
            {
                get
                {
                    return IsFullyLoaded && Asset != null && Weight > 0f;
                }
            }

            readonly ExpansionDef[] _baseRequiredExpansions = [];

            public ReadOnlyArray<ExpansionDef> RequiredExpansions { get; private set; }

            public bool IsFullyLoaded { get; private set; }

            public event OnEntryLoadedDelegate OnEntryLoaded
            {
                add
                {
                    if (IsFullyLoaded)
                    {
                        value(this);
                        return;
                    }

                    _onEntryLoaded += value;
                }
                remove
                {
                    _onEntryLoaded -= value;
                }
            }

            event OnEntryLoadedDelegate _onEntryLoaded;

            Entry(T asset, SpawnPoolEntryParameters parameters, bool fullyLoaded)
            {
                Asset = asset;
                Weight = parameters.Weight;
                _baseRequiredExpansions = parameters.RequiredExpansions;
                IsFullyLoaded = fullyLoaded;

                RequiredExpansions = new ReadOnlyArray<ExpansionDef>(_baseRequiredExpansions);
            }

            public Entry(T asset, SpawnPoolEntryParameters parameters) : this(asset, parameters, true)
            {
            }

            public void SetRequiredExpansions(IReadOnlyCollection<ExpansionDef> requiredExpansions)
            {
                List<ExpansionDef> distinctExpansions = new List<ExpansionDef>(requiredExpansions.Count);
                foreach (ExpansionDef expansionDef in requiredExpansions)
                {
                    if (Array.IndexOf(_baseRequiredExpansions, expansionDef) == -1)
                    {
                        distinctExpansions.Add(expansionDef);
                    }
                }

                RequiredExpansions = new ReadOnlyArray<ExpansionDef>([.. _baseRequiredExpansions, .. distinctExpansions]);
            }

            public override string ToString()
            {
                return $"{Asset}: {Weight} ({string.Join(", ", RequiredExpansions)})";
            }

            public static Entry LoadAsync<TAsset>(string assetGuid, SpawnPoolEntryParameters parameters, Converter<TAsset, T> converter) where TAsset : UnityEngine.Object
            {
                Entry entry = new Entry(default, parameters, false);

                AsyncOperationHandle<TAsset> assetLoad = AddressableUtil.LoadAssetAsync<TAsset>(assetGuid);
                assetLoad.OnSuccess(asset =>
                {
                    entry.Asset = converter(asset);
                    entry.onFullyLoaded();
                });

                return entry;
            }

            public static Entry LoadAsync(string assetGuid, SpawnPoolEntryParameters parameters)
            {
                return LoadAsync<T>(assetGuid, parameters, v => v);
            }

            void onFullyLoaded()
            {
                IsFullyLoaded = true;
                _onEntryLoaded?.Invoke(this);
                _onEntryLoaded = null;
            }
        }
    }
}
