using RiskOfChaos.Utilities.Extensions;
using RoR2.ExpansionManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities
{
    public sealed class SpawnPool<T> : IReadOnlyCollection<T>
    {
        static readonly WeightedSelection<T> _sharedSpawnSelection = new WeightedSelection<T>();

        readonly List<Entry> _entries = [];

#if DEBUG
        public bool DebugTest;
        int _debugTestIndex = 0;
#endif

        public delegate ExpansionDef[] RequiredExpansionsProviderDelegate(T entry);

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
                    if (!entry.IsAvailable)
                        continue;

                    if (!ExpansionUtils.AllExpansionsEnabled(entry.RequiredExpansions))
                        continue;

                    bool isAvailable = true;
                    if (CalcIsEntryAvailable != null)
                    {
                        foreach (IsEntryAvailableDelegate isAvailableDelegate in CalcIsEntryAvailable.GetInvocationList())
                        {
                            if (!isAvailableDelegate(entry.Asset))
                            {
                                isAvailable = false;
                                break;
                            }
                        }
                    }

                    return isAvailable;
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
                if (entry.IsAvailable)
                {
                    weightedSelection.AddChoice(entry.Asset, entry.Weight);
                }
            }
        }

        public T PickRandomEntry(Xoroshiro128Plus rng)
        {
#if DEBUG
            if (DebugTest)
            {
                return _entries[_debugTestIndex++ % _entries.Count].Asset;
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
            entry.RequiredExpansions = _requiredExpansionsProvider(entry.Asset);
        }

        public void AddEntry(Entry entry)
        {
            _entries.Add(entry);

            entry.CallWhenLoaded(refreshRequiredExpansions);
        }

        public void AddEntry(T asset, float weight)
        {
            AddEntry(new Entry(asset, weight));
        }

        public Entry LoadEntry(string assetPath, float weight)
        {
            return Entry.LoadAsync(assetPath, weight);
        }

        public void AddAssetEntry(string assetPath, float weight)
        {
            AddEntry(LoadEntry(assetPath, weight));
        }

        public Entry LoadEntry<TAsset>(string assetPath, float weight, Converter<TAsset, T> assetConverter)
        {
            return Entry.LoadAsync(assetPath, weight, assetConverter);
        }

        public void AddAssetEntry<TAsset>(string assetPath, float weight, Converter<TAsset, T> assetConverter)
        {
            AddEntry(LoadEntry(assetPath, weight, assetConverter));
        }

        public Entry LoadEntry(string assetPath, float weight, Converter<T, T> assetConverter)
        {
            return LoadEntry<T>(assetPath, weight, assetConverter);
        }

        public void AddAssetEntry(string assetPath, float weight, Converter<T, T> assetConverter)
        {
            AddAssetEntry<T>(assetPath, weight, assetConverter);
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

            public bool IsAvailable
            {
                get
                {
                    return IsFullyLoaded && Asset != null && Weight > 0f;
                }
            }

            public ExpansionDef[] RequiredExpansions = [];

            public bool IsFullyLoaded { get; private set; }
            readonly List<OnEntryLoadedDelegate> _onLoadedListeners = [];

            Entry(T asset, float weight, bool fullyLoaded)
            {
                Asset = asset;
                Weight = weight;
                IsFullyLoaded = fullyLoaded;
            }

            public Entry(T asset, float weight) : this(asset, weight, true)
            {
            }

            public override string ToString()
            {
                return $"{Asset}: {Weight} ({string.Join<ExpansionDef>(", ", RequiredExpansions)})";
            }

            public static Entry LoadAsync<TAsset>(string path, float weight, Converter<TAsset, T> converter)
            {
                Entry entry = new Entry(default, weight, false);

                AsyncOperationHandle<TAsset> assetLoad = Addressables.LoadAssetAsync<TAsset>(path);
                assetLoad.OnSuccess(asset =>
                {
                    entry.Asset = converter(asset);
                    entry.onFullyLoaded();
                });

                return entry;
            }

            public static Entry LoadAsync(string path, float weight)
            {
                return LoadAsync<T>(path, weight, v => v);
            }

            void onFullyLoaded()
            {
                IsFullyLoaded = true;
                foreach (OnEntryLoadedDelegate onLoadedListener in _onLoadedListeners)
                {
                    onLoadedListener(this);
                }

                _onLoadedListeners.Clear();
                _onLoadedListeners.TrimExcess();
            }

            public void CallWhenLoaded(OnEntryLoadedDelegate onLoaded)
            {
                if (IsFullyLoaded)
                {
                    onLoaded(this);
                    return;
                }

                _onLoadedListeners.Add(onLoaded);
            }
        }
    }
}
