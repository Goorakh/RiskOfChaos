using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities
{
    public sealed class SpawnPool<T> : ICollection<AssetOrDirectReference<T>>, IReadOnlyCollection<AssetOrDirectReference<T>> where T : UnityEngine.Object
    {
        static readonly WeightedSelection<AssetOrDirectReference<T>> _sharedSpawnSelection = new WeightedSelection<AssetOrDirectReference<T>>();

        readonly List<SpawnPoolEntry<T>> _entries = [];

#if DEBUG
        public bool DebugTest;
        int _debugTestIndex = 0;
#endif

        public bool AnyAvailable
        {
            get
            {
                foreach (SpawnPoolEntry<T> entry in _entries)
                {
                    if (entry.IsAvailable)
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

        bool ICollection<AssetOrDirectReference<T>>.IsReadOnly => true;

        void generateSpawnSelection(WeightedSelection<AssetOrDirectReference<T>> weightedSelection)
        {
            weightedSelection.Clear();
            weightedSelection.EnsureCapacity(_entries.Count);

            foreach (SpawnPoolEntry<T> entry in _entries)
            {
                if (entry.IsAvailable)
                {
                    weightedSelection.AddChoice(entry.GetAssetReference(false), entry.Weight);
                }
            }
        }

        public AssetOrDirectReference<T> PickRandomEntry(Xoroshiro128Plus rng)
        {
            // Always take the next float from the rng, so the caller can have deterministic results with the same rng instance
            float normalizedIndex = rng.nextNormalizedFloat;

#if DEBUG
            if (DebugTest)
            {
                SpawnPoolEntry<T> entry;
                do
                {
                    entry = _entries[_debugTestIndex++ % _entries.Count];
                } while (!entry.IsAvailable);

                return entry.GetAssetReference();
            }
#endif

            generateSpawnSelection(_sharedSpawnSelection);
            AssetOrDirectReference<T> assetReference = _sharedSpawnSelection.Evaluate(normalizedIndex);
            assetReference.LoadAsync();
            return assetReference;
        }

        public WeightedSelection<AssetOrDirectReference<T>> GetSpawnSelection()
        {
            WeightedSelection<AssetOrDirectReference<T>> spawnSelection = new WeightedSelection<AssetOrDirectReference<T>>();
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

#if DEBUG
        public void DebugPrintEntries()
        {
            SpawnPoolEntry<T>[] entries = [.. _entries];
            Array.Sort(entries, (a, b) =>
            {
                ReadOnlyArray<ExpansionIndex> expansionsA = a.RequiredExpansions;
                ReadOnlyArray<ExpansionIndex> expansionsB = b.RequiredExpansions;

                if (expansionsA.Length != expansionsB.Length)
                {
                    return expansionsB.Length - expansionsA.Length;
                }

                for (int i = 0; i < expansionsA.Length; i++)
                {
                    ExpansionDef expansionDefA = ExpansionUtils.GetExpansionDef(expansionsA[i]);
                    ExpansionDef expansionDefB = ExpansionUtils.GetExpansionDef(expansionsB[i]);
                    int compare = string.Compare(expansionDefA?.name, expansionDefB?.name, true);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }

                return 0;
            });

            foreach (SpawnPoolEntry<T> entry in entries)
            {
                Log.Debug_NoCallerPrefix(entry);
            }
        }
#endif

        public void AddEntry(SpawnPoolEntry<T> entry)
        {
            _entries.Add(entry);
        }

        public void AddEntry(T asset, SpawnPoolEntryParameters parameters)
        {
            AddEntry(new SpawnPoolEntry<T>(asset, parameters));
        }

        public SpawnPoolEntry<T> CreateEntry(T asset, SpawnPoolEntryParameters parameters)
        {
            return new SpawnPoolEntry<T>(asset, parameters);
        }

        public SpawnPoolEntry<T> LoadEntry(string assetGuid, SpawnPoolEntryParameters parameters)
        {
            return new SpawnPoolEntry<T>(assetGuid, parameters);
        }

        public void AddAssetEntry(string assetGuid, SpawnPoolEntryParameters parameters)
        {
            AddEntry(LoadEntry(assetGuid, parameters));
        }

        public SpawnPoolEntry<T> LoadEntry<TAsset>(string assetGuid, SpawnPoolEntryParameters parameters, Converter<TAsset, T> assetConverter) where TAsset : UnityEngine.Object
        {
            return SpawnPoolEntry<T>.CreateConvertedAssetEntry(assetGuid, assetConverter, parameters);
        }

        public void AddAssetEntry<TAsset>(string assetGuid, SpawnPoolEntryParameters parameters, Converter<TAsset, T> assetConverter) where TAsset : UnityEngine.Object
        {
            AddEntry(LoadEntry(assetGuid, parameters, assetConverter));
        }

        public SpawnPoolEntry<T> LoadEntry(string assetGuid, SpawnPoolEntryParameters parameters, Converter<T, T> assetConverter)
        {
            return LoadEntry<T>(assetGuid, parameters, assetConverter);
        }

        public void AddAssetEntry(string assetGuid, SpawnPoolEntryParameters parameters, Converter<T, T> assetConverter)
        {
            AddAssetEntry<T>(assetGuid, parameters, assetConverter);
        }

        public SpawnPoolEntry<T>[] GroupEntries(SpawnPoolEntry<T>[] entries, float weightMultiplier = 1f)
        {
            foreach (SpawnPoolEntry<T> entry in entries)
            {
                entry.Weight *= weightMultiplier / entries.Length;
            }

            return entries;
        }

        public void AddGroupedEntries(SpawnPoolEntry<T>[] entries, float weightMultiplier = 1f)
        {
            EnsureCapacity(Count + entries.Length);

            foreach (SpawnPoolEntry<T> entry in GroupEntries(entries, weightMultiplier))
            {
                AddEntry(entry);
            }
        }

        void ICollection<AssetOrDirectReference<T>>.Add(AssetOrDirectReference<T> item)
        {
            throw new NotSupportedException("Collection is read-only");
        }

        void ICollection<AssetOrDirectReference<T>>.Clear()
        {
            throw new NotSupportedException("Collection is read-only");
        }

        public bool Contains(AssetOrDirectReference<T> item)
        {
            foreach (SpawnPoolEntry<T> entry in _entries)
            {
                if (entry.MatchAssetReference(item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(AssetOrDirectReference<T>[] array, int arrayIndex)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                SpawnPoolEntry<T> entry = _entries[i];
                array[arrayIndex + i] = entry.GetAssetReference(false);
            }
        }

        bool ICollection<AssetOrDirectReference<T>>.Remove(AssetOrDirectReference<T> item)
        {
            throw new NotSupportedException("Collection is read-only");
        }

        public IEnumerator<AssetOrDirectReference<T>> GetEnumerator()
        {
            return _entries.Select(e => e.GetAssetReference(false)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
